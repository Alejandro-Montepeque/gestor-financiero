using GestorFinanciero.Application.DTOs;

namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Read-only aggregations powering the home dashboard.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetCurrentMonthStatsAsync(CancellationToken ct = default);
}
