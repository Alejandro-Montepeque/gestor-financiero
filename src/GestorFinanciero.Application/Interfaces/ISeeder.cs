namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// System-level seeder: a piece of setup that runs at most once per environment
/// (demo user, default admin, seed data, etc.). Executions are tracked in the
/// <c>seeder_executions</c> table so each key runs exactly once.
/// </summary>
/// <remarks>
/// This is different from <see cref="ICategorySeeder"/>, which runs every time
/// a new user registers. <see cref="ISeeder"/> is one-shot system seeding.
/// Naming convention for <see cref="Key"/> is "NNNN_Name" so string ordering
/// gives us execution order (e.g. "0001_SeedRoles", "0002_SeedDemoUser").
/// </remarks>
public interface ISeeder
{
    /// <summary>Unique, stable identifier. Never change once it has been shipped.</summary>
    string Key { get; }

    Task RunAsync(IServiceProvider services, CancellationToken cancellationToken);
}
