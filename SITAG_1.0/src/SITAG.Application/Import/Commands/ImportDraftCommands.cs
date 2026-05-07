using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Import.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;
using System.Text.Json;

namespace SITAG.Application.Import.Commands;

// ── Shared row model (deserialized from RowsJson during Confirm) ───────────────
internal sealed record ImportRow(
    int RowIndex,
    string? TagNumber,
    string? Name,
    string? Breed,
    string Sex,
    string? BirthDate,       // "yyyy-MM-dd" or null
    decimal? Weight,
    string? Color,
    Guid? FarmId,
    Guid? DivisionId,
    string? MotherRef,
    Guid? MotherId,
    string? FatherRef,
    Guid? FatherId,
    int InsertOrder          // pre-calculated by frontend topological sort
);

// ── Shared mapper ──────────────────────────────────────────────────────────────
internal static class ImportDraftMapper
{
    internal static ImportDraftDto ToDto(ImportDraft d) => new(
        d.Id, d.FileName, d.Status.ToString(), d.RowsJson, d.ExpiresAt, d.CreatedAt);
}

// ── CreateOrReplace ────────────────────────────────────────────────────────────
public sealed record CreateOrReplaceImportDraftCommand(
    string FileName,
    string RowsJson) : IRequest<ImportDraftDto>;

public sealed class CreateOrReplaceImportDraftHandler
    : IRequestHandler<CreateOrReplaceImportDraftCommand, ImportDraftDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public CreateOrReplaceImportDraftHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    public async Task<ImportDraftDto> Handle(CreateOrReplaceImportDraftCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        // Cancel any existing pending draft for this tenant
        var existing = await _db.ImportDrafts
            .Where(d => d.TenantId == tid && d.Status == ImportDraftStatus.Pending)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            existing.Status = ImportDraftStatus.Cancelled;
        }

        var draft = new ImportDraft
        {
            TenantId  = tid,
            FileName  = r.FileName.Trim(),
            RowsJson  = r.RowsJson,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        };

        _db.ImportDrafts.Add(draft);
        await _db.SaveChangesAsync(ct);
        return ImportDraftMapper.ToDto(draft);
    }
}

// ── UpdateRows ─────────────────────────────────────────────────────────────────
public sealed record UpdateImportDraftRowsCommand(
    Guid DraftId,
    string RowsJson) : IRequest<ImportDraftDto>;

public sealed class UpdateImportDraftRowsHandler
    : IRequestHandler<UpdateImportDraftRowsCommand, ImportDraftDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateImportDraftRowsHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    public async Task<ImportDraftDto> Handle(UpdateImportDraftRowsCommand r, CancellationToken ct)
    {
        var draft = await _db.ImportDrafts
            .FirstOrDefaultAsync(d => d.Id == r.DraftId && d.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Draft {r.DraftId} no encontrado.");

        if (draft.Status != ImportDraftStatus.Pending)
            throw new InvalidOperationException("Solo se pueden editar drafts en estado Pending.");

        draft.RowsJson = r.RowsJson;
        await _db.SaveChangesAsync(ct);
        return ImportDraftMapper.ToDto(draft);
    }
}

// ── Cancel ─────────────────────────────────────────────────────────────────────
public sealed record CancelImportDraftCommand(Guid DraftId) : IRequest;

public sealed class CancelImportDraftHandler : IRequestHandler<CancelImportDraftCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public CancelImportDraftHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    public async Task Handle(CancelImportDraftCommand r, CancellationToken ct)
    {
        var draft = await _db.ImportDrafts
            .FirstOrDefaultAsync(d => d.Id == r.DraftId && d.TenantId == _user.TenantId, ct)
            ?? throw new KeyNotFoundException($"Draft {r.DraftId} no encontrado.");

        if (draft.Status != ImportDraftStatus.Pending)
            throw new InvalidOperationException("Solo se pueden cancelar drafts en estado Pending.");

        draft.Status = ImportDraftStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Confirm ────────────────────────────────────────────────────────────────────
public sealed record ConfirmImportDraftCommand(Guid DraftId) : IRequest<ImportConfirmResultDto>;

public sealed class ConfirmImportDraftHandler
    : IRequestHandler<ConfirmImportDraftCommand, ImportConfirmResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public ConfirmImportDraftHandler(IApplicationDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    public async Task<ImportConfirmResultDto> Handle(ConfirmImportDraftCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        var draft = await _db.ImportDrafts
            .FirstOrDefaultAsync(d => d.Id == r.DraftId && d.TenantId == tid, ct)
            ?? throw new KeyNotFoundException($"Draft {r.DraftId} no encontrado.");

        if (draft.Status != ImportDraftStatus.Pending)
            throw new InvalidOperationException("Este draft ya fue confirmado o cancelado.");

        var rows = JsonSerializer.Deserialize<List<ImportRow>>(draft.RowsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        // Process in topological order (InsertOrder pre-calculated by frontend)
        var ordered = rows.OrderBy(r => r.InsertOrder).ToList();

        // Track newly inserted animals by TagNumber so internal deps resolve correctly
        var insertedByTag = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        var rowResults = new List<ImportRowResultDto>();

        foreach (var row in ordered)
        {
            try
            {
                if (!row.FarmId.HasValue)
                    throw new InvalidOperationException("Finca no asignada — asigne una finca antes de confirmar.");

                // Resolve internal parent references
                var motherId = row.MotherId;
                if (motherId == null && row.MotherRef is not null &&
                    insertedByTag.TryGetValue(row.MotherRef, out var internalMotherId))
                    motherId = internalMotherId;

                var fatherId = row.FatherId;
                if (fatherId == null && row.FatherRef is not null &&
                    insertedByTag.TryGetValue(row.FatherRef, out var internalFatherId))
                    fatherId = internalFatherId;

                var effectiveTag = string.IsNullOrWhiteSpace(row.TagNumber) ? null : row.TagNumber.Trim();

                if (effectiveTag != null &&
                    await _db.Animals.AnyAsync(a => a.TagNumber == effectiveTag && a.TenantId == tid, ct))
                    throw new InvalidOperationException($"El arete '{effectiveTag}' ya existe en el sistema.");

                DateOnly? birthDate = null;
                if (!string.IsNullOrWhiteSpace(row.BirthDate) &&
                    DateOnly.TryParse(row.BirthDate, out var parsedDate))
                    birthDate = parsedDate;

                var animal = new Animal
                {
                    TenantId   = tid,
                    TagNumber  = effectiveTag,
                    Name       = row.Name?.Trim(),
                    Breed      = row.Breed?.Trim(),
                    Sex        = row.Sex,
                    BirthDate  = birthDate,
                    Weight     = row.Weight,
                    Color      = row.Color?.Trim(),
                    FarmId     = row.FarmId.Value,
                    DivisionId = row.DivisionId,
                    MotherId   = motherId,
                    MotherRef  = row.MotherRef?.Trim(),
                    FatherId   = fatherId,
                    FatherRef  = row.FatherRef?.Trim(),
                };

                _db.Animals.Add(animal);
                await _db.SaveChangesAsync(ct);

                if (effectiveTag is not null)
                    insertedByTag[effectiveTag] = animal.Id;

                rowResults.Add(new ImportRowResultDto(row.RowIndex, true, animal.Id, null));
            }
            catch (Exception ex)
            {
                rowResults.Add(new ImportRowResultDto(row.RowIndex, false, null, ex.Message));
            }
        }

        draft.Status = ImportDraftStatus.Confirmed;
        await _db.SaveChangesAsync(ct);

        return new ImportConfirmResultDto(
            Total: rowResults.Count,
            Succeeded: rowResults.Count(r => r.Success),
            Failed: rowResults.Count(r => !r.Success),
            Rows: rowResults
        );
    }
}
