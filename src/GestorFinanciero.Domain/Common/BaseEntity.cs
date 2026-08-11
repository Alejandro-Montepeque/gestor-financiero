namespace GestorFinanciero.Domain.Common;

/// <summary>
/// Base class for all domain entities. Uses <see cref="Guid"/> as the primary key
/// to make entities independent of the database sequence generator.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
