using GestorFinanciero.Domain.Enums;

namespace GestorFinanciero.Application.DTOs;

/// <summary>Snapshot of the current month shown on the home dashboard.</summary>
public sealed record DashboardStatsDto(
    // ── Current month totals ──
    decimal IncomeThisMonth,
    decimal ExpensesThisMonth,
    decimal SavingsThisMonth,
    decimal Balance,
    string Currency,
    int TransactionCount,

    // ── Widgets ──
    IReadOnlyList<RecentTransactionDto> RecentTransactions,
    IReadOnlyList<CategoryBreakdownDto> ExpensesByCategory,

    // ── Deltas vs previous month (in absolute currency units) ──
    decimal IncomeDelta,
    decimal ExpensesDelta,
    decimal BalanceDelta,
    string PreviousMonthName,

    // ── Expense split by CategoryType ──
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal DebtExpenses,

    // ── Health metrics ──
    decimal SavingsRatePercent,  // (income - expenses) / income * 100, clamped [0,100]
    decimal AvgDailySpend,        // expenses / days elapsed
    int DaysElapsed,
    int DaysInMonth);

/// <summary>Compact projection of a transaction for the "recent activity" widget.</summary>
public sealed record RecentTransactionDto(
    Guid Id,
    DateOnly Date,
    string CategoryName,
    string CategoryColor,
    CategoryType CategoryType,
    string? Description,
    decimal Amount,
    string Currency);

/// <summary>One row of the "expenses by category" breakdown.</summary>
public sealed record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    decimal Amount,
    decimal Percentage);
