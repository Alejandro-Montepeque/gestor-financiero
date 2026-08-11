using GestorFinanciero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorFinanciero.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(80).IsRequired();
        builder.Property(c => c.Type).HasConversion<int>().IsRequired();
        builder.Property(c => c.Color).HasMaxLength(9).IsRequired();
        builder.Property(c => c.Icon).HasMaxLength(50).IsRequired();
        builder.Property(c => c.IsSystem).IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();

        // Index: fast lookup of a user's categories, and filtering by type.
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.Type });
    }
}
