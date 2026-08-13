using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class AppEventConfiguration : IEntityTypeConfiguration<AppEvent>
{
    public void Configure(EntityTypeBuilder<AppEvent> builder)
    {
        builder.ToTable("app_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Module).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(40).IsRequired();
        builder.Property(e => e.Level).HasMaxLength(16).IsRequired();
        builder.Property(e => e.Message).HasColumnType("text").IsRequired();

        builder.Property(e => e.ExceptionType).HasMaxLength(300);
        builder.Property(e => e.StackTrace).HasColumnType("text");
        builder.Property(e => e.FailureReason).HasMaxLength(80);

        builder.Property(e => e.UserEmail).HasMaxLength(320);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.RequestPath).HasMaxLength(500);
        builder.Property(e => e.RequestMethod).HasMaxLength(10);

        builder.Property(e => e.Extra).HasColumnType("text");

        // Indexes for the typical queries:
        //  - Timeline of everything ordered by time.
        //  - "What happened in this module lately?" → (Module, OccurredAt).
        //  - "What did this user do?" → (UserId, OccurredAt).
        //  - Suspicious activity per email → (UserEmail, OccurredAt).
        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => new { e.Module, e.OccurredAt });
        builder.HasIndex(e => new { e.Level, e.OccurredAt });
        builder.HasIndex(e => new { e.UserId, e.OccurredAt });
        builder.HasIndex(e => new { e.UserEmail, e.OccurredAt });
    }
}
