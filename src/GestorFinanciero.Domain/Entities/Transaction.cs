using GestorFinanciero.Domain.Common;
using GestorFinanciero.Domain.ValueObjects;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// A single financial movement (income or expense) recorded by the user.
/// The direction (in/out) is inferred from the linked <see cref="Category"/>'s
/// <see cref="Enums.CategoryType"/>, so <see cref="Amount"/> stays non-negative.
/// </summary>
public class Transaction : BaseAuditableEntity
{
    public Guid UserId { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>Non-negative monetary amount. Sign is derived from the category type.</summary>
    public Money Amount { get; set; } = Money.Zero();

    /// <summary>When the transaction actually happened (not when it was recorded).</summary>
    public DateOnly Date { get; set; }

    /// <summary>Short label shown in lists (e.g. "Netflix").</summary>
    public string? Description { get; set; }

    /// <summary>Optional long-form notes.</summary>
    public string? Notes { get; set; }

    // Navigation
    public Category? Category { get; set; }
}
