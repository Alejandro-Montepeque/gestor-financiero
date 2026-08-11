using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class BudgetItemConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    public void Configure(EntityTypeBuilder<BudgetItem> builder)
    {
        builder.ToTable("budget_items");

        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.BudgetId).IsRequired();
        builder.Property(bi => bi.CategoryId).IsRequired();
        builder.Property(bi => bi.CreatedAt).IsRequired();

        // Ignore derived properties — they are computed from the two Money columns.
        builder.Ignore(bi => bi.Difference);
        builder.Ignore(bi => bi.PercentUsed);

        // Estimated amount (Money value object).
        builder.OwnsOne(bi => bi.EstimatedAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("estimated_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("estimated_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Actual amount (Money value object).
        builder.OwnsOne(bi => bi.ActualAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("actual_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("actual_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Relationships. Deleting the parent budget removes all items (cascade).
        builder.HasOne(bi => bi.Budget)
               .WithMany(b => b.Items)
               .HasForeignKey(bi => bi.BudgetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bi => bi.Category)
               .WithMany(c => c.BudgetItems)
               .HasForeignKey(bi => bi.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // Only one item per (budget, category).
        builder.HasIndex(bi => new { bi.BudgetId, bi.CategoryId }).IsUnique();
    }
}
