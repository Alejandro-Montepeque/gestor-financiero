using GestorFinanciero.Application.DTOs;

namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// Application-level operations against the current user's categories.
/// All methods resolve the user id internally through <see cref="ICurrentUserService"/>,
/// so callers never risk leaking another user's data.
/// </summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);

    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<CategoryDto> CreateAsync(CategoryUpsertDto input, CancellationToken ct = default);

    Task<CategoryDto> UpdateAsync(CategoryUpsertDto input, CancellationToken ct = default);

    /// <summary>Deletes a category. Fails if the category still has transactions.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
