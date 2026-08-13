namespace GestorFinanciero.Domain.Common;

/// <summary>
/// Base class for entities that need audit trail (created/updated timestamps and users).
/// The <see cref="CreatedAt"/> value is set at construction; <see cref="UpdatedAt"/> is
/// populated by <c>AppDbContext.SaveChangesAsync</c> whenever the entity is modified.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
