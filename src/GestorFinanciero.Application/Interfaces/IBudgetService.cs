using GestorFinanciero.Application.DTOs;

namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Monthly budget CRUD. Users pick a (year, month) tuple; the service returns
/// a grid row per category with the estimate they saved (if any) and the
/// actual amount pulled from the month's transactions.
/// </summary>
public interface IBudgetService
{
    Task<BudgetDto> GetForMonthAsync(int year, int month, CancellationToken ct = default);

    Task<BudgetDto> SaveEstimatesAsync(
        int year,
        int month,
        IEnumerable<BudgetEstimateUpsertDto> estimates,
        CancellationToken ct = default);

    Task DeleteAsync(int year, int month, CancellationToken ct = default);
}
