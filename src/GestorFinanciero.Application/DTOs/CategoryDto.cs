using GestorFinanciero.Domain.Enums;

namespace GestorFinanciero.Application.DTOs;

/// <summary>Read model for a category (list/detail views).</summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    CategoryType Type,
    string Color,
    string Icon,
    bool IsSystem);

/// <summary>
/// Write model for creating or updating a category. Same shape for both operations —
/// on create, <see cref="Id"/> is ignored; on update, it must be provided.
/// </summary>
public sealed class CategoryUpsertDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; } = CategoryType.VariableExpense;
    public string Color { get; set; } = "#7C3AED";
    public string Icon { get; set; } = "circle";
}
