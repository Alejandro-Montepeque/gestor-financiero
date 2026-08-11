using System.Diagnostics;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestorFinanciero.Infrastructure.Seeding;

/// <summary>
/// Orchestrates <see cref="ISeeder"/> execution on startup. Reads the
/// <c>seeder_executions</c> table, filters out anything already run, and
/// executes the remaining seeders in <see cref="ISeeder.Key"/> order.
/// Each successful execution is recorded so it won't run again.
/// </summary>
public sealed class SeederRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SeederRunner> _logger;

    public SeederRunner(IServiceScopeFactory scopeFactory, ILogger<SeederRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeders = scope.ServiceProvider.GetServices<ISeeder>().ToList();

        if (seeders.Count == 0)
        {
            _logger.LogInformation("No seeders registered — skipping.");
            return;
        }

        var alreadyRun = await db.Set<SeederExecution>()
            .AsNoTracking()
            .Select(e => e.Key)
            .ToHashSetAsync(cancellationToken);

        var pending = seeders
            .Where(s => !alreadyRun.Contains(s.Key))
            .OrderBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

        if (pending.Count == 0)
        {
            _logger.LogInformation("All {Count} seeders already executed.", seeders.Count);
            return;
        }

        _logger.LogInformation("Running {Pending} pending seeder(s) of {Total} registered.", pending.Count, seeders.Count);

        foreach (var seeder in pending)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await seeder.RunAsync(scope.ServiceProvider, cancellationToken);
                sw.Stop();

                db.Set<SeederExecution>().Add(new SeederExecution
                {
                    Key = seeder.Key,
                    ExecutedAt = DateTime.UtcNow,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                });
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Seeder '{Key}' executed in {Ms}ms.", seeder.Key, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Seeder '{Key}' failed after {Ms}ms — remaining seeders are aborted.",
                    seeder.Key, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
