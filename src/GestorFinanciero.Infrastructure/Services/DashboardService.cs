using System.Globalization;
using GestorFinanciero.Application.DTOs;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Enums;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanciero.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private const int RecentTransactionsCount = 5;
    private const int TopExpenseCategoriesCount = 5;

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-ES");

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardStatsDto> GetCurrentMonthStatsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var previousMonthStart = monthStart.AddMonths(-1);

        // ── 1. Load transactions of the last 2 months in one round trip. ──
        var rows = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                    && t.Date >= previousMonthStart
                    && t.Date < nextMonthStart)
            .Select(t => new
            {
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency,
                Date = t.Date,
                CategoryId = t.CategoryId,
                CategoryName = t.Category!.Name,
                CategoryColor = t.Category.Color,
                CategoryType = t.Category.Type,
            })
            .ToListAsync(ct);

        var current = rows.Where(r => r.Date >= monthStart && r.Date < nextMonthStart).ToList();
        var previous = rows.Where(r => r.Date >= previousMonthStart && r.Date < monthStart).ToList();

        var (income, expenses, savings, fixedExp, varExp, debtExp) = SumBuckets(current);
        var (prevIncome, prevExpenses, _, _, _, _) = SumBuckets(previous);

        var balance = income - expenses;
        var prevBalance = prevIncome - prevExpenses;

        // ── 2. Recent transactions (across all history, not only this month) ──
        var recent = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(RecentTransactionsCount)
            .Select(t => new RecentTransactionDto(
                t.Id,
                t.Date,
                t.Category!.Name,
                t.Category.Color,
                t.Category.Type,
                t.Description,
                t.Amount.Amount,
                t.Amount.Currency))
            .ToListAsync(ct);

        // ── 3. Expenses by category (top N by amount) ──
        var expenseGroups = current
            .Where(r => IsExpense(r.CategoryType))
            .GroupBy(r => new { r.CategoryId, r.CategoryName, r.CategoryColor })
            .Select(g => new CategoryBreakdownDto(
                g.Key.CategoryId,
                g.Key.CategoryName,
                g.Key.CategoryColor,
                g.Sum(r => r.Amount),
                expenses > 0 ? Math.Round(g.Sum(r => r.Amount) / expenses * 100m, 1) : 0m))
            .OrderByDescending(c => c.Amount)
            .Take(TopExpenseCategoriesCount)
            .ToList();

        // ── 4. Health metrics ──
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var daysElapsed = today.Day;
        var avgDailySpend = daysElapsed > 0 ? Math.Round(expenses / daysElapsed, 2) : 0m;

        // Savings rate: how much of income is left over. Clamped so it renders
        // nicely in a progress ring even when expenses > income.
        var savingsRate = income > 0
            ? Math.Clamp(Math.Round((income - expenses) / income * 100m, 1), 0m, 100m)
            : 0m;

        var currency = current.FirstOrDefault()?.Currency
                    ?? recent.FirstOrDefault()?.Currency
                    ?? "USD";

        return new DashboardStatsDto(
            IncomeThisMonth:      income,
            ExpensesThisMonth:    expenses,
            SavingsThisMonth:     savings,
            Balance:              balance,
            Currency:             currency,
            TransactionCount:     current.Count,
            RecentTransactions:   recent,
            ExpensesByCategory:   expenseGroups,
            IncomeDelta:          income - prevIncome,
            ExpensesDelta:        expenses - prevExpenses,
            BalanceDelta:         balance - prevBalance,
            PreviousMonthName:    Culture.DateTimeFormat.GetMonthName(previousMonthStart.Month),
            FixedExpenses:        fixedExp,
            VariableExpenses:     varExp,
            DebtExpenses:         debtExp,
            SavingsRatePercent:   savingsRate,
            AvgDailySpend:        avgDailySpend,
            DaysElapsed:          daysElapsed,
            DaysInMonth:          daysInMonth);
    }

    private static (decimal income, decimal expenses, decimal savings,
                    decimal fixedExp, decimal varExp, decimal debtExp)
        SumBuckets(IEnumerable<dynamic> rows)
    {
        decimal income = 0, expenses = 0, savings = 0, fixedExp = 0, varExp = 0, debtExp = 0;

        foreach (var r in rows)
        {
            var amount = (decimal)r.Amount;
            var type = (CategoryType)r.CategoryType;
            switch (type)
            {
                case CategoryType.Income:          income += amount; break;
                case CategoryType.Savings:         savings += amount; break;
                case CategoryType.FixedExpense:    fixedExp += amount; expenses += amount; break;
                case CategoryType.VariableExpense: varExp += amount;   expenses += amount; break;
                case CategoryType.Debt:            debtExp += amount;  expenses += amount; break;
            }
        }

        return (income, expenses, savings, fixedExp, varExp, debtExp);
    }

    private static bool IsExpense(CategoryType t) =>
        t is CategoryType.FixedExpense or CategoryType.VariableExpense or CategoryType.Debt;
}
