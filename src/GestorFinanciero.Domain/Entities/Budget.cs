using GestorFinanciero.Domain.Common;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// A monthly budget. Each budget aggregates one <see cref="BudgetItem"/> per
/// category, where the user records an estimate and the system tracks the
/// actual spend for the period.
/// </summary>
public class Budget : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public int Year { get; set; }

    /// <summary>1-based month number (1 = January, 12 = December).</summary>
    public int Month { get; set; }

    public ICollection<BudgetItem> Items { get; set; } = new List<BudgetItem>();
}
