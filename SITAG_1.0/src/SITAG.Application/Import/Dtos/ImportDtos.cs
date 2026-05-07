namespace SITAG.Application.Import.Dtos;

public record ImportDraftDto(
    Guid Id,
    string FileName,
    string Status,
    string RowsJson,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt
);

// Per-row result returned after ConfirmDraft
public record ImportRowResultDto(
    int RowIndex,
    bool Success,
    Guid? AnimalId,
    string? Error
);

public record ImportConfirmResultDto(
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<ImportRowResultDto> Rows
);
