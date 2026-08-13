using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("debts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(120).IsRequired();
        builder.Property(d => d.InterestRate).HasColumnType("numeric(5,2)");
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.Property(d => d.CreatedAt).IsRequired();

        // Current balance (required).
        builder.OwnsOne(d => d.CurrentBalance, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("current_balance_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("current_balance_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // Minimum payment (nullable — some debts have no minimum).
        builder.OwnsOne(d => d.MinimumPayment, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("minimum_payment_amount")
                 .HasColumnType("numeric(18,2)");

            money.Property(m => m.Currency)
                 .HasColumnName("minimum_payment_currency")
                 .HasMaxLength(3);
        });

        builder.HasIndex(d => d.UserId);
    }
}
