using GestorFinanciero.Domain.Common;
using GestorFinanciero.Domain.ValueObjects;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// A single line of a monthly <see cref="Budget"/>: the estimate for one category
/// paired with the actual amount spent during the period.
/// </summary>
public class BudgetItem : BaseAuditableEntity
{
    public Guid BudgetId { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>What the user planned to spend / earn.</summary>
    public Money EstimatedAmount { get; set; } = Money.Zero();

    /// <summary>
    /// What actually happened. Recomputed by the application service whenever
    /// transactions in the period change.
    /// </summary>
    public Money ActualAmount { get; set; } = Money.Zero();

    // Navigation
    public Budget? Budget { get; set; }
    public Category? Category { get; set; }

    /// <summary>Estimate minus actual. Positive means under-budget.</summary>
    public decimal Difference => EstimatedAmount.Amount - ActualAmount.Amount;

    /// <summary>Percentage of the estimate that has been used (0 if no estimate).</summary>
    public decimal PercentUsed => EstimatedAmount.Amount == 0m
        ? 0m
        : ActualAmount.Amount / EstimatedAmount.Amount * 100m;
}
