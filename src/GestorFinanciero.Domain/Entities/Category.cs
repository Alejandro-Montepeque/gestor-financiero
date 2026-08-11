using GestorFinanciero.Domain.Common;
using GestorFinanciero.Domain.Enums;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// A category groups transactions by financial purpose. Every user has their own
/// categories, but the system seeds a default set on registration
/// (e.g. Salary, Rent, Groceries, Savings).
/// </summary>
public class Category : BaseAuditableEntity
{
    /// <summary>Owner of the category. Categories are per-user, never global.</summary>
    public Guid UserId { get; set; }

    /// <summary>Display name (e.g. "Salario", "Alquiler"). Localised at the UI layer.</summary>
    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; }

    /// <summary>Hex colour used for charts and badges (e.g. "#7C3AED").</summary>
    public string Color { get; set; } = "#7C3AED";

    /// <summary>Lucide icon name (e.g. "wallet", "shopping-bag").</summary>
    public string Icon { get; set; } = "circle";

    /// <summary>
    /// System-provided categories cannot be deleted (only hidden). This flag is
    /// true for the default set seeded on user registration.
    /// </summary>
    public bool IsSystem { get; set; }

    // Navigation
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
}
