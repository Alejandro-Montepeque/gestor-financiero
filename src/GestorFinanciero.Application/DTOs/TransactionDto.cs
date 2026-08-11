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

/// <summary>Simple filter args for the list page (date range + optional category).</summary>
public sealed class TransactionFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public Guid? CategoryId { get; set; }
    public int Take { get; set; } = 100;
}
