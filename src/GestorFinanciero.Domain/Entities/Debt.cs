using GestorFinanciero.Domain.Common;
using GestorFinanciero.Domain.ValueObjects;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// An outstanding debt tracked by the user (credit card, personal loan, etc.).
/// The current balance decreases as <see cref="DebtPayment"/>s are applied.
/// </summary>
public class Debt : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Outstanding principal at the last recorded payment.</summary>
    public Money CurrentBalance { get; set; } = Money.Zero();

    /// <summary>Annual interest rate as a percentage (e.g. 18.5 for 18.5% APR).</summary>
    public decimal? InterestRate { get; set; }

    /// <summary>Contractually required payment per period, if any.</summary>
    public Money? MinimumPayment { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public ICollection<DebtPayment> Payments { get; set; } = new List<DebtPayment>();
}
