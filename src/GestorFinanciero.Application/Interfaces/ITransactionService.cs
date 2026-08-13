using GestorFinanciero.Application.DTOs;

namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Application-level operations against the current user's transactions.
/// Ownership is enforced internally through <see cref="ICurrentUserService"/>.
/// </summary>
public interface ITransactionService
{
    Task<IReadOnlyList<TransactionDto>> ListAsync(TransactionFilter filter, CancellationToken ct = default);

    /// <summary>Export up to 10 000 rows respecting the same filters as <see cref="ListAsync"/>.</summary>
    Task<IReadOnlyList<TransactionDto>> ExportAsync(TransactionFilter filter, CancellationToken ct = default);

    Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<TransactionDto> CreateAsync(TransactionUpsertDto input, CancellationToken ct = default);

    Task<TransactionDto> UpdateAsync(TransactionUpsertDto input, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
