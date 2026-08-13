using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.CategoryId).IsRequired();
        builder.Property(t => t.Date).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(255);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.CreatedAt).IsRequired();

        // Money value object mapped as two columns (amount + currency).
        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Relationship: many transactions per category. Deleting a category is
        // blocked while it still has transactions (Restrict).
        builder.HasOne(t => t.Category)
               .WithMany(c => c.Transactions)
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        // Indexes optimised for typical queries:
        //  - "transactions for user X, ordered by date desc"  → (UserId, Date)
        //  - "spending by category in period"                 → (UserId, CategoryId, Date)
        builder.HasIndex(t => new { t.UserId, t.Date });
        builder.HasIndex(t => new { t.UserId, t.CategoryId, t.Date });
    }
}
