using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("pending_registrations");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Email).HasMaxLength(256).IsRequired();
        builder.Property(pr => pr.FullName).HasMaxLength(120).IsRequired();
        builder.Property(pr => pr.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(pr => pr.ExpiresAt).IsRequired();
        builder.Property(pr => pr.AttemptsCount).IsRequired();
        builder.Property(pr => pr.IsVerified).IsRequired();
        builder.Property(pr => pr.CreatedAt).IsRequired();

        // Only one pending registration per email at a time — on retry we upsert.
        builder.HasIndex(pr => pr.Email).IsUnique();

        // Fast lookup from the SetPassword page after verification.
        builder.HasIndex(pr => pr.VerificationToken).IsUnique();
    }
}
