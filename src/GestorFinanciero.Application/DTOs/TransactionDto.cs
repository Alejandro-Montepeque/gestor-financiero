using GestorFinanciero.Domain.Enums;

namespace GestorFinanciero.Application.DTOs;

/// <summary>Read model for a transaction — includes the joined category info for display.</summary>
public sealed record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string? Description,
    string? Notes,
    Guid CategoryId,
    string CategoryName,
    CategoryType CategoryType,
    string CategoryColor,
    string CategoryIcon);

/// <summary>Write model for creating or updating a transaction.</summary>
public sealed class TransactionUpsertDto
{
    public Guid? Id { get; set; }

    public Guid CategoryId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public string? Description { get; set; }

    public string? Notes { get; set; }
}

/// <summary>Filter args for the list page: date range, category, category type,
/// amount range, and a free-text search over description/notes.</summary>
public sealed class TransactionFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? CategoryId { get; set; }
    public CategoryType? CategoryType { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    /// <summary>Case-insensitive substring match over Description + Notes.</summary>
    public string? Search { get; set; }

    /// <summary>Cap for the result set. Enforced server-side (max 500).</summary>
    public int Take { get; set; } = 100;
}
