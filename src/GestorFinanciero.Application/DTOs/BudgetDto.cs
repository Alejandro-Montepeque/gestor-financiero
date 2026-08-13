using GestorFinanciero.Domain.Enums;

namespace GestorFinanciero.Application.DTOs;

/// <summary>A single row of the monthly budget grid.</summary>
public sealed record BudgetItemDto(
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    CategoryType CategoryType,
    decimal EstimatedAmount,
    decimal ActualAmount,
    string Currency)
{
    public decimal Difference   => EstimatedAmount - ActualAmount;
    public decimal PercentUsed  => EstimatedAmount == 0 ? 0 : ActualAmount / EstimatedAmount * 100m;
    public bool OverBudget      => EstimatedAmount > 0 && ActualAmount > EstimatedAmount;
}

/// <summary>Full budget view for a given (year, month) — includes ALL of the
/// user's categories, even those without a stored estimate yet.</summary>
public sealed record BudgetDto(
    int Year,
    int Month,
    string Currency,
    IReadOnlyList<BudgetItemDto> Items)
{
    public decimal TotalEstimated => Items.Sum(i => i.EstimatedAmount);
    public decimal TotalActual    => Items.Sum(i => i.ActualAmount);
}

/// <summary>Payload used to save a single estimate on the budget grid.</summary>
public sealed class BudgetEstimateUpsertDto
{
    public Guid CategoryId { get; set; }
    public decimal EstimatedAmount { get; set; }
}
