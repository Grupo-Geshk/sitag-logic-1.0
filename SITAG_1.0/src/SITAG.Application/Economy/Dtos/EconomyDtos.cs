using SITAG.Domain.Enums;

namespace SITAG.Application.Economy.Dtos;

public sealed record TransactionCategoryDto(Guid Id, string Name, TransactionType Type, bool IsActive);

public sealed record TransactionDto(
    Guid Id,
    Guid TenantId,
    TransactionType Type,
    Guid? CategoryId,
    string? CategoryName,
    string? Description,
    decimal Amount,
    DateTimeOffset TxnDate,
    Guid? FarmId,
    DateTimeOffset CreatedAt);

public sealed record EconomySummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<CategoryBreakdownDto> ByCategory,
    IReadOnlyList<FarmBreakdownDto> ByFarm);

public sealed record EconomyTrendPointDto(
    string Period,          // e.g. "2025-01" (monthly) or "2025-W03" (weekly)
    DateTimeOffset PeriodStart,
    decimal Income,
    decimal Expense,
    decimal Balance);

public sealed record CategoryBreakdownDto(
    Guid? CategoryId,
    string CategoryName,
    decimal Income,
    decimal Expense,
    decimal Balance);

public sealed record FarmBreakdownDto(
    Guid? FarmId,
    string FarmName,
    decimal Income,
    decimal Expense,
    decimal Balance);
