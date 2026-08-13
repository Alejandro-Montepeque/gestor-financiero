using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.UserId).IsRequired();
        builder.Property(b => b.Year).IsRequired();
        builder.Property(b => b.Month).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();

        // A user can only have ONE budget per (year, month).
        builder.HasIndex(b => new { b.UserId, b.Year, b.Month }).IsUnique();
    }
}
