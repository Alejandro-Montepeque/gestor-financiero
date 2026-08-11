using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Domain.Enums;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanciero.Infrastructure.Seeding;

/// <summary>
/// Default implementation of <see cref="ICategorySeeder"/> backed by
/// <see cref="AppDbContext"/>. Seeds the five system categories every user
/// starts with on registration.
/// </summary>
public sealed class CategorySeeder : ICategorySeeder
{
    private readonly AppDbContext _db;

    public CategorySeeder(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Default set: one income category and four expense buckets.</summary>
    private static readonly (string Name, CategoryType Type, string Color, string Icon)[] Defaults =
    [
        ("Salario",          CategoryType.Income,          "#22C55E", "wallet"),
        ("Alquiler",         CategoryType.FixedExpense,    "#EF4444", "home"),
        ("Alimentos",        CategoryType.VariableExpense, "#F59E0B", "shopping-bag"),
        ("Ahorros",          CategoryType.Savings,         "#3B82F6", "piggy-bank"),
        ("Otros",            CategoryType.VariableExpense, "#A855F7", "circle-ellipsis"),
    ];

    public async Task SeedForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Idempotent: never seed twice for the same user.
        var alreadySeeded = await _db.Categories
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId, cancellationToken);

        if (alreadySeeded) return;

        var categories = Defaults.Select(d => new Category
        {
            UserId = userId,
            Name = d.Name,
            Type = d.Type,
            Color = d.Color,
            Icon = d.Icon,
            IsSystem = true,
        });

        await _db.Categories.AddRangeAsync(categories, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
