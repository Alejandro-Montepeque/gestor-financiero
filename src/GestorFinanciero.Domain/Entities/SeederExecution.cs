using GestorFinanciero.Domain.Common;

namespace GestorFinanciero.Domain.Entities;

/// <summary>
/// Historical record of a system-level seeder that has already been executed.
/// The <see cref="SeederRunner"/> reads this table on startup and skips any
/// seeder whose <see cref="Key"/> is already present, so seeds run exactly once
/// per environment.
/// </summary>
public class SeederExecution : BaseEntity
{
    /// <summary>Unique identifier of the seeder (e.g. "0001_SeedDemoUser").</summary>
    public string Key { get; set; } = string.Empty;

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public int DurationMs { get; set; }
}
