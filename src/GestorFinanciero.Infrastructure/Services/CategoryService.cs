using GestorFinanciero.Application.DTOs;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanciero.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CategoryService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color, c.Icon, c.IsSystem))
            .ToListAsync(ct);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id && c.UserId == userId)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Type, c.Color, c.Icon, c.IsSystem))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<CategoryDto> CreateAsync(CategoryUpsertDto input, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var entity = new Category
        {
            UserId = userId,
            Name = input.Name.Trim(),
            Type = input.Type,
            Color = input.Color,
            Icon = input.Icon,
            IsSystem = false,
        };

        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new CategoryDto(entity.Id, entity.Name, entity.Type, entity.Color, entity.Icon, entity.IsSystem);
    }

    public async Task<CategoryDto> UpdateAsync(CategoryUpsertDto input, CancellationToken ct = default)
    {
        if (input.Id is null)
            throw new ArgumentException("Category id is required for update.", nameof(input));

        var userId = _currentUser.GetUserId();

        var entity = await _db.Categories
            .SingleOrDefaultAsync(c => c.Id == input.Id && c.UserId == userId, ct)
            ?? throw new InvalidOperationException("Category not found.");

        entity.Name = input.Name.Trim();
        entity.Type = input.Type;
        entity.Color = input.Color;
        entity.Icon = input.Icon;

        await _db.SaveChangesAsync(ct);

        return new CategoryDto(entity.Id, entity.Name, entity.Type, entity.Color, entity.Icon, entity.IsSystem);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var entity = await _db.Categories
            .SingleOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct)
            ?? throw new InvalidOperationException("Category not found.");

        if (entity.IsSystem)
            throw new InvalidOperationException("System categories cannot be deleted.");

        // Guard: block deletion if the category still has transactions attached.
        var hasTransactions = await _db.Transactions
            .AnyAsync(t => t.CategoryId == id, ct);

        if (hasTransactions)
            throw new InvalidOperationException(
                "Cannot delete a category that still has transactions. Reassign them first.");

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
