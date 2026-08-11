using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class DebtPaymentConfiguration : IEntityTypeConfiguration<DebtPayment>
{
    public void Configure(EntityTypeBuilder<DebtPayment> builder)
    {
        builder.ToTable("debt_payments");

        builder.HasKey(dp => dp.Id);

        builder.Property(dp => dp.DebtId).IsRequired();
        builder.Property(dp => dp.Date).IsRequired();
        builder.Property(dp => dp.CreatedAt).IsRequired();

        // TotalPaid is calculated (Minimum + Extra), not persisted.
        builder.Ignore(dp => dp.TotalPaid);

        builder.OwnsOne(dp => dp.MinimumPayment, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("minimum_payment_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("minimum_payment_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        builder.OwnsOne(dp => dp.ExtraPayment, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("extra_payment_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("extra_payment_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        builder.OwnsOne(dp => dp.InterestAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("interest_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("interest_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        builder.OwnsOne(dp => dp.PrincipalAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("principal_amount")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("principal_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // RemainingBalance is optional (some payments happen without recomputing balance).
        builder.OwnsOne(dp => dp.RemainingBalance, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("remaining_balance_amount")
                 .HasColumnType("numeric(18,2)");

            money.Property(m => m.Currency)
                 .HasColumnName("remaining_balance_currency")
                 .HasMaxLength(3);
        });

        // Cascade: delete payments when the parent debt is deleted.
        builder.HasOne(dp => dp.Debt)
               .WithMany(d => d.Payments)
               .HasForeignKey(dp => dp.DebtId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dp => new { dp.DebtId, dp.Date });
    }
}
