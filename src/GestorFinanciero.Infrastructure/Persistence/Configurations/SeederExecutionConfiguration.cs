using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class SeederExecutionConfiguration : IEntityTypeConfiguration<SeederExecution>
{
    public void Configure(EntityTypeBuilder<SeederExecution> builder)
    {
        builder.ToTable("seeder_executions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key).HasMaxLength(150).IsRequired();
        builder.Property(e => e.ExecutedAt).IsRequired();
        builder.Property(e => e.DurationMs).IsRequired();

        // A seeder key must be unique — enforced at the DB layer so a race
        // between two starting instances can never insert the same key twice.
        builder.HasIndex(e => e.Key).IsUnique();
    }
}
