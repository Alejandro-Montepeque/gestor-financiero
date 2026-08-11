using GestorFinanciero.Application.DTOs;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Domain.ValueObjects;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanciero.Infrastructure.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TransactionService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TransactionDto>> ListAsync(TransactionFilter filter, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var query = _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (filter.From is { } from)
            query = query.Where(t => t.Date >= from);

        if (filter.To is { } to)
            query = query.Where(t => t.Date <= to);

        if (filter.CategoryId is { } categoryId)
            query = query.Where(t => t.CategoryId == categoryId);

        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(Math.Clamp(filter.Take, 1, 500))
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Date,
                t.Description,
                t.Notes,
                t.CategoryId,
                t.Category!.Name,
                t.Category.Type,
                t.Category.Color,
                t.Category.Icon))
            .ToListAsync(ct);
    }

    public async Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        return await _db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Date,
                t.Description,
                t.Notes,
                t.CategoryId,
                t.Category!.Name,
                t.Category.Type,
                t.Category.Color,
                t.Category.Icon))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<TransactionDto> CreateAsync(TransactionUpsertDto input, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        // Guard: category must exist and belong to the current user.
        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == input.CategoryId && c.UserId == userId, ct);

        if (!categoryExists)
            throw new InvalidOperationException("Category not found.");

        var entity = new Transaction
        {
            UserId = userId,
            CategoryId = input.CategoryId,
            Amount = new Money(input.Amount, input.Currency),
            Date = input.Date,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
        };

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Reload with the category info projected in.
        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task<TransactionDto> UpdateAsync(TransactionUpsertDto input, CancellationToken ct = default)
    {
        if (input.Id is null)
            throw new ArgumentException("Transaction id is required for update.", nameof(input));

        var userId = _currentUser.GetUserId();

        var entity = await _db.Transactions
            .SingleOrDefaultAsync(t => t.Id == input.Id && t.UserId == userId, ct)
            ?? throw new InvalidOperationException("Transaction not found.");

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == input.CategoryId && c.UserId == userId, ct);

        if (!categoryExists)
            throw new InvalidOperationException("Category not found.");

        entity.CategoryId = input.CategoryId;
        entity.Amount = new Money(input.Amount, input.Currency);
        entity.Date = input.Date;
        entity.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        await _db.SaveChangesAsync(ct);

        return (await GetByIdAsync(entity.Id, ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var entity = await _db.Transactions
            .SingleOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new InvalidOperationException("Transaction not found.");

        _db.Transactions.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
