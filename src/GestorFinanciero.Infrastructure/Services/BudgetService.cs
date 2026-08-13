using GestorFinanciero.Application.DTOs;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Domain.ValueObjects;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanciero.Infrastructure.Services;

public sealed class BudgetService : IBudgetService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BudgetService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BudgetDto> GetForMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Color, c.Type })
            .ToListAsync(ct);

        var estimates = await _db.BudgetItems
            .AsNoTracking()
            .Where(bi => bi.Budget!.UserId == userId
                      && bi.Budget.Year == year
                      && bi.Budget.Month == month)
            .Select(bi => new
            {
                bi.CategoryId,
                Amount   = bi.EstimatedAmount.Amount,
                Currency = bi.EstimatedAmount.Currency,
            })
            .ToListAsync(ct);

        var estimatesByCategory = estimates.ToDictionary(e => e.CategoryId, e => e);

        var (monthStart, nextMonthStart) = MonthRange(year, month);
        var actuals = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= monthStart && t.Date < nextMonthStart)
            .GroupBy(t => t.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Amount     = g.Sum(t => t.Amount.Amount),
                Currency   = g.Max(t => t.Amount.Currency),
            })
            .ToListAsync(ct);

        var actualsByCategory = actuals.ToDictionary(a => a.CategoryId, a => a);

        var currency = estimates.FirstOrDefault()?.Currency
                    ?? actuals.FirstOrDefault()?.Currency
                    ?? "USD";

        var items = categories.Select(c =>
        {
            var estimated = estimatesByCategory.TryGetValue(c.Id, out var est) ? est.Amount : 0m;
            var actual    = actualsByCategory.TryGetValue(c.Id, out var act)   ? act.Amount : 0m;
            return new BudgetItemDto(
                CategoryId:      c.Id,
                CategoryName:    c.Name,
                CategoryColor:   c.Color,
                CategoryType:    c.Type,
                EstimatedAmount: estimated,
                ActualAmount:    actual,
                Currency:        currency);
        }).ToList();

        return new BudgetDto(year, month, currency, items);
    }

    public async Task<BudgetDto> SaveEstimatesAsync(
        int year,
        int month,
        IEnumerable<BudgetEstimateUpsertDto> estimates,
        CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();
        var payload = estimates?.ToList() ?? [];

        // ── Step 1: get (or create) the Budget row in its own transaction ──
        // Persisting the Budget first guarantees we have a stable Id before
        // touching BudgetItems, and it lets EF track them independently.
        var budget = await _db.Budgets
            .SingleOrDefaultAsync(b => b.UserId == userId && b.Year == year && b.Month == month, ct);

        if (budget is null)
        {
            budget = new Budget { UserId = userId, Year = year, Month = month };
            _db.Budgets.Add(budget);
            await _db.SaveChangesAsync(ct);
        }

        // ── Step 2: apply the item changes ──
        var existingItems = await _db.BudgetItems
            .Where(bi => bi.BudgetId == budget.Id)
            .ToListAsync(ct);

        var existingByCategory = existingItems.ToDictionary(i => i.CategoryId);
        var currency = existingItems.FirstOrDefault()?.EstimatedAmount.Currency ?? "USD";

        // Preload the user's category ids so we can validate without N round trips.
        var userCategoryIds = await _db.Categories
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToListAsync(ct);
        var userCategorySet = userCategoryIds.ToHashSet();

        foreach (var edit in payload)
        {
            if (!userCategorySet.Contains(edit.CategoryId)) continue;

            if (edit.EstimatedAmount <= 0m)
            {
                if (existingByCategory.TryGetValue(edit.CategoryId, out var toRemove))
                    _db.BudgetItems.Remove(toRemove);
                continue;
            }

            if (existingByCategory.TryGetValue(edit.CategoryId, out var existing))
            {
                existing.EstimatedAmount = new Money(edit.EstimatedAmount, currency);
            }
            else
            {
                _db.BudgetItems.Add(new BudgetItem
                {
                    BudgetId = budget.Id,
                    CategoryId = edit.CategoryId,
                    EstimatedAmount = new Money(edit.EstimatedAmount, currency),
                    ActualAmount = new Money(0m, currency),
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return await GetForMonthAsync(year, month, ct);
    }

    public async Task DeleteAsync(int year, int month, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        // Remove items first (no cascade in this path — cascade only applies on
        // DB-level FK, and going through the tracker avoids ambiguity here).
        var items = await _db.BudgetItems
            .Where(bi => bi.Budget!.UserId == userId
                      && bi.Budget.Year == year
                      && bi.Budget.Month == month)
            .ToListAsync(ct);

        if (items.Count > 0)
        {
            _db.BudgetItems.RemoveRange(items);
        }

        var budget = await _db.Budgets
            .SingleOrDefaultAsync(b => b.UserId == userId && b.Year == year && b.Month == month, ct);

        if (budget is not null)
        {
            _db.Budgets.Remove(budget);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static (DateOnly monthStart, DateOnly nextMonthStart) MonthRange(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return (start, start.AddMonths(1));
    }
}
