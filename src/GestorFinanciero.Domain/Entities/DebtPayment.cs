using GestorFinanciero.Domain.Common;
using GestorFinanciero.Domain.ValueObjects;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// A single payment applied to a <see cref="Debt"/>. Splits the amount into the
/// interest and principal portions so amortisation reports can be built on top.
/// </summary>
public class DebtPayment : BaseAuditableEntity
{
    public Guid DebtId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Portion of the payment that covers the contracted minimum.</summary>
    public Money MinimumPayment { get; set; } = Money.Zero();

    /// <summary>Extra amount paid on top of the minimum (accelerates payoff).</summary>
    public Money ExtraPayment { get; set; } = Money.Zero();

    /// <summary>Portion of the total payment that went to interest.</summary>
    public Money InterestAmount { get; set; } = Money.Zero();

    /// <summary>Portion of the total payment that went to reducing principal.</summary>
    public Money PrincipalAmount { get; set; } = Money.Zero();

    /// <summary>Debt balance immediately after this payment was applied.</summary>
    public Money? RemainingBalance { get; set; }

    // Navigation
    public Debt? Debt { get; set; }

    /// <summary>Convenience helper: total cash outflow for this payment.</summary>
    public Money TotalPaid => MinimumPayment + ExtraPayment;
}
