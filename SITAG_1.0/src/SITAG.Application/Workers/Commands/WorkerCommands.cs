using MediatR;
using Microsoft.EntityFrameworkCore;
using SITAG.Application.Common.Interfaces;
using SITAG.Application.Workers.Dtos;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Application.Workers.Commands;

// ── Create ────────────────────────────────────────────────────────────────────
public sealed record CreateWorkerCommand(
    string Name, string? RoleLabel, string? Contact) : IRequest<WorkerDto>;

public sealed class CreateWorkerHandler : IRequestHandler<CreateWorkerCommand, WorkerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateWorkerHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerDto> Handle(CreateWorkerCommand r, CancellationToken ct)
    {
        var w = new Worker
        {
            TenantId  = _user.TenantId,
            Name      = r.Name.Trim(),
            RoleLabel = r.RoleLabel?.Trim(),
            Contact   = r.Contact?.Trim(),
        };
        _db.Workers.Add(w);
        await _db.SaveChangesAsync(ct);
        return new WorkerDto(w.Id, w.TenantId, w.Name, w.RoleLabel, w.Contact, w.Status, w.CreatedAt, []);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────
public sealed record UpdateWorkerCommand(
    Guid WorkerId, string Name, string? RoleLabel, string? Contact) : IRequest<WorkerDto>;

public sealed class UpdateWorkerHandler : IRequestHandler<UpdateWorkerCommand, WorkerDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UpdateWorkerHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerDto> Handle(UpdateWorkerCommand r, CancellationToken ct)
    {
        var w = await _db.Workers
            .FirstOrDefaultAsync(w => w.Id == r.WorkerId && w.TenantId == _user.TenantId && w.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        w.Name      = r.Name.Trim();
        w.RoleLabel = r.RoleLabel?.Trim();
        w.Contact   = r.Contact?.Trim();
        await _db.SaveChangesAsync(ct);

        var farmIds = await _db.WorkerFarmAssignments
            .Where(a => a.WorkerId == w.Id && a.EndDate == null)
            .Select(a => a.FarmId).ToListAsync(ct);
        return new WorkerDto(w.Id, w.TenantId, w.Name, w.RoleLabel, w.Contact, w.Status, w.CreatedAt, farmIds);
    }
}

// ── Toggle status ─────────────────────────────────────────────────────────────
public sealed record SetWorkerStatusCommand(Guid WorkerId, WorkerStatus Status) : IRequest;

public sealed class SetWorkerStatusHandler : IRequestHandler<SetWorkerStatusCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public SetWorkerStatusHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(SetWorkerStatusCommand r, CancellationToken ct)
    {
        var w = await _db.Workers
            .FirstOrDefaultAsync(w => w.Id == r.WorkerId && w.TenantId == _user.TenantId && w.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");
        w.Status = r.Status;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Assign to farms ───────────────────────────────────────────────────────────
public sealed record AssignWorkerFarmsCommand(Guid WorkerId, List<Guid> FarmIds) : IRequest;

public sealed class AssignWorkerFarmsHandler : IRequestHandler<AssignWorkerFarmsCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public AssignWorkerFarmsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(AssignWorkerFarmsCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var workerExists = await _db.Workers
            .AnyAsync(w => w.Id == r.WorkerId && w.TenantId == tid && w.DeletedAt == null, ct);
        if (!workerExists) throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingFarmIds = await _db.WorkerFarmAssignments
            .Where(a => a.WorkerId == r.WorkerId && a.TenantId == tid && a.EndDate == null)
            .Select(a => a.FarmId)
            .ToListAsync(ct);

        foreach (var fid in r.FarmIds.Except(existingFarmIds))
        {
            _db.WorkerFarmAssignments.Add(new WorkerFarmAssignment
            {
                TenantId        = tid,
                WorkerId        = r.WorkerId,
                FarmId          = fid,
                StartDate       = today,
                CreatedByUserId = _user.UserId,
            });
        }
        await _db.SaveChangesAsync(ct);
    }
}

// ── Unassign from farms ───────────────────────────────────────────────────────
public sealed record UnassignWorkerFarmsCommand(Guid WorkerId, List<Guid> FarmIds) : IRequest;

public sealed class UnassignWorkerFarmsHandler : IRequestHandler<UnassignWorkerFarmsCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public UnassignWorkerFarmsHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(UnassignWorkerFarmsCommand r, CancellationToken ct)
    {
        var tid  = _user.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assignments = await _db.WorkerFarmAssignments
            .Where(a => a.WorkerId == r.WorkerId && a.TenantId == tid
                     && r.FarmIds.Contains(a.FarmId) && a.EndDate == null)
            .ToListAsync(ct);

        foreach (var a in assignments) a.EndDate = today;
        await _db.SaveChangesAsync(ct);
    }
}

// ── Register payment ──────────────────────────────────────────────────────────
public sealed record CreateWorkerPaymentCommand(
    Guid WorkerId,
    PaymentMode Mode,
    DateOnly PaymentDate,
    decimal Rate,
    decimal Quantity,
    string? Notes,
    Guid? FarmId) : IRequest<WorkerPaymentDto>;

public sealed class CreateWorkerPaymentHandler : IRequestHandler<CreateWorkerPaymentCommand, WorkerPaymentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateWorkerPaymentHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerPaymentDto> Handle(CreateWorkerPaymentCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;

        var worker = await _db.Workers
            .FirstOrDefaultAsync(w => w.Id == r.WorkerId && w.TenantId == tid && w.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        var total = Math.Round(r.Rate * r.Quantity, 2);
        var modeLabel = r.Mode == PaymentMode.Hourly ? "hora" : "día";

        // Auto-create economy transaction
        var txn = new EconomyTransaction
        {
            TenantId     = tid,
            Type         = TransactionType.Egreso,
            CategoryName = "ManoDeObra",
            Description  = $"Pago por {modeLabel} — {worker.Name}" + (string.IsNullOrWhiteSpace(r.Notes) ? "" : $": {r.Notes}"),
            Amount       = total,
            TxnDate      = new DateTimeOffset(r.PaymentDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            FarmId       = r.FarmId,
            CreatedByUserId = _user.UserId,
        };
        _db.EconomyTransactions.Add(txn);

        var payment = new WorkerPayment
        {
            TenantId    = tid,
            WorkerId    = r.WorkerId,
            Mode        = r.Mode,
            PaymentDate = r.PaymentDate,
            Rate        = r.Rate,
            Quantity    = r.Quantity,
            TotalAmount = total,
            Notes       = r.Notes?.Trim(),
            FarmId      = r.FarmId,
        };
        _db.WorkerPayments.Add(payment);

        await _db.SaveChangesAsync(ct);

        // Link payment → transaction now that we have IDs
        payment.TransactionId = txn.Id;
        await _db.SaveChangesAsync(ct);

        return new WorkerPaymentDto(
            payment.Id, payment.WorkerId, payment.Mode, payment.PaymentDate,
            payment.Rate, payment.Quantity, payment.TotalAmount,
            payment.Notes, payment.FarmId, payment.TransactionId, payment.CreatedAt);
    }
}

// ── Register loan ─────────────────────────────────────────────────────────────
public sealed record CreateWorkerLoanCommand(
    Guid WorkerId, decimal Amount, DateOnly LoanDate, string? Description) : IRequest<WorkerLoanDto>;

public sealed class CreateWorkerLoanHandler : IRequestHandler<CreateWorkerLoanCommand, WorkerLoanDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public CreateWorkerLoanHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerLoanDto> Handle(CreateWorkerLoanCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var worker = await _db.Workers
            .FirstOrDefaultAsync(w => w.Id == r.WorkerId && w.TenantId == tid && w.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        var loan = new WorkerLoan
        {
            TenantId        = tid,
            WorkerId        = r.WorkerId,
            Amount          = Math.Round(r.Amount, 2),
            RemainingAmount = Math.Round(r.Amount, 2),
            LoanDate        = r.LoanDate,
            Description     = r.Description?.Trim(),
        };
        _db.WorkerLoans.Add(loan);
        await _db.SaveChangesAsync(ct);

        return new WorkerLoanDto(loan.Id, loan.WorkerId, loan.Amount, loan.RemainingAmount, loan.LoanDate, loan.Description, loan.CreatedAt);
    }
}

// ── Pay loan ──────────────────────────────────────────────────────────────────
public sealed record PayWorkerLoanCommand(
    Guid WorkerId, Guid LoanId, decimal Amount) : IRequest<WorkerLoanDto>;

public sealed class PayWorkerLoanHandler : IRequestHandler<PayWorkerLoanCommand, WorkerLoanDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public PayWorkerLoanHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task<WorkerLoanDto> Handle(PayWorkerLoanCommand r, CancellationToken ct)
    {
        var tid = _user.TenantId;
        var loan = await _db.WorkerLoans
            .FirstOrDefaultAsync(l => l.Id == r.LoanId && l.WorkerId == r.WorkerId && l.TenantId == tid, ct)
            ?? throw new KeyNotFoundException($"Loan {r.LoanId} not found.");

        if (r.Amount <= 0 || r.Amount > loan.RemainingAmount)
            throw new InvalidOperationException("Payment amount is invalid.");

        loan.RemainingAmount = Math.Round(loan.RemainingAmount - r.Amount, 2);
        await _db.SaveChangesAsync(ct);

        return new WorkerLoanDto(loan.Id, loan.WorkerId, loan.Amount, loan.RemainingAmount, loan.LoanDate, loan.Description, loan.CreatedAt);
    }
}

// ── Soft delete ───────────────────────────────────────────────────────────────
public sealed record DeleteWorkerCommand(Guid WorkerId) : IRequest;

public sealed class DeleteWorkerHandler : IRequestHandler<DeleteWorkerCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    public DeleteWorkerHandler(IApplicationDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public async Task Handle(DeleteWorkerCommand r, CancellationToken ct)
    {
        var w = await _db.Workers
            .FirstOrDefaultAsync(w => w.Id == r.WorkerId && w.TenantId == _user.TenantId && w.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Worker {r.WorkerId} not found.");

        // Close all active farm assignments
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var active = await _db.WorkerFarmAssignments
            .Where(a => a.WorkerId == r.WorkerId && a.TenantId == _user.TenantId && a.EndDate == null)
            .ToListAsync(ct);
        foreach (var a in active) a.EndDate = today;

        w.Status    = WorkerStatus.Inactivo;
        w.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
