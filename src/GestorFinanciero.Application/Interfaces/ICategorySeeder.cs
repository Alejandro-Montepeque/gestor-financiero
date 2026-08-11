namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Populates a user's category list with a sensible default set the first time
/// they sign up (Salario, Alquiler, Alimentos, Ahorros, Otros).
/// </summary>
public interface ICategorySeeder
{
    /// <summary>
    /// Idempotent: safe to call more than once for the same user.
    /// If the user already has categories, this is a no-op.
    /// </summary>
    Task SeedForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
