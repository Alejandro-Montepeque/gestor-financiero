using GestorFinanciero.Application.DTOs;
using GestorFinanciero.Application.Interfaces;
using GestorFinanciero.Domain.Entities;
using GestorFinanciero.Domain.ValueObjects;
using GestorFinanciero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestorFinanciero.Infrastructure.Services;

public sealed class DebtService : IDebtService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DebtService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DebtDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var rows = await _db.Debts
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                Balance  = d.CurrentBalance.Amount,
                Currency = d.CurrentBalance.Currency,
                d.InterestRate,
                MinAmount = (decimal?)(d.MinimumPayment != null ? d.MinimumPayment.Amount : (decimal?)null),
                d.Notes,
                Payments = d.Payments.Select(p => new { p.Date }).ToList(),
            })
            .ToListAsync(ct);

        return rows.Select(r => new DebtDto(
            Id:               r.Id,
            Name:             r.Name,
            CurrentBalance:   r.Balance,
            Currency:         r.Currency,
            InterestRate:     r.InterestRate,
            MinimumPayment:   r.MinAmount,
            Notes:            r.Notes,
            PaymentCount:     r.Payments.Count,
            LastPaymentDate:  r.Payments.Count == 0 ? null : r.Payments.Max(p => p.Date))).ToList();
    }

    public async Task<DebtDetailDto?> GetByIdAsync(Guid debtId, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var debt = await _db.Debts
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == debtId && d.UserId == userId, ct);

        if (debt is null) return null;

        var payments = await _db.DebtPayments
            .AsNoTracking()
            .Where(p => p.DebtId == debt.Id)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new DebtPaymentDto(
                Id:               p.Id,
                Date:             p.Date,
                MinimumPayment:   p.MinimumPayment.Amount,
                ExtraPayment:     p.ExtraPayment.Amount,
                InterestAmount:   p.InterestAmount.Amount,
                PrincipalAmount:  p.PrincipalAmount.Amount,
                RemainingBalance: p.RemainingBalance != null ? p.RemainingBalance.Amount : (decimal?)null,
                Currency:         p.MinimumPayment.Currency))
            .ToListAsync(ct);

        var dto = new DebtDto(
            Id:              debt.Id,
            Name:            debt.Name,
            CurrentBalance:  debt.CurrentBalance.Amount,
            Currency:        debt.CurrentBalance.Currency,
            InterestRate:    debt.InterestRate,
            MinimumPayment:  debt.MinimumPayment?.Amount,
            Notes:           debt.Notes,
            PaymentCount:    payments.Count,
            LastPaymentDate: payments.FirstOrDefault()?.Date);

        return new DebtDetailDto(dto, payments);
    }

    public async Task<DebtDto> CreateAsync(DebtUpsertDto input, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var entity = new Debt
        {
            UserId         = userId,
            Name           = input.Name.Trim(),
            CurrentBalance = new Money(input.CurrentBalance, input.Currency),
            InterestRate   = input.InterestRate,
            MinimumPayment = input.MinimumPayment is > 0m
                                ? new Money(input.MinimumPayment.Value, input.Currency)
                                : null,
            Notes          = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
        };

        _db.Debts.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new DebtDto(
            entity.Id, entity.Name,
            entity.CurrentBalance.Amount, entity.CurrentBalance.Currency,
            entity.InterestRate, entity.MinimumPayment?.Amount, entity.Notes,
            PaymentCount: 0, LastPaymentDate: null);
    }

    public async Task<DebtDto> UpdateAsync(DebtUpsertDto input, CancellationToken ct = default)
    {
        if (input.Id is null)
            throw new ArgumentException("Debt id is required for update.", nameof(input));

        var userId = _currentUser.GetUserId();

        // Don't Include(Payments) here — modifying the parent while a nav
        // collection is loaded triggers a "0 rows affected" concurrency error
        // in EF Core 10 when we replace OwnsOne value objects.
        var entity = await _db.Debts
            .SingleOrDefaultAsync(d => d.Id == input.Id && d.UserId == userId, ct)
            ?? throw new InvalidOperationException("Debt not found.");

        entity.Name           = input.Name.Trim();
        entity.CurrentBalance = new Money(input.CurrentBalance, input.Currency);
        entity.InterestRate   = input.InterestRate;
        entity.MinimumPayment = input.MinimumPayment is > 0m
                                    ? new Money(input.MinimumPayment.Value, input.Currency)
                                    : null;
        entity.Notes          = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        await _db.SaveChangesAsync(ct);

        // Fetch counts separately so we don't disturb the tracker.
        var paymentCount    = await _db.DebtPayments.CountAsync(p => p.DebtId == entity.Id, ct);
        var lastPaymentDate = paymentCount == 0 ? (DateOnly?)null : await _db.DebtPayments
            .Where(p => p.DebtId == entity.Id)
            .MaxAsync(p => p.Date, ct);

        return new DebtDto(
            entity.Id, entity.Name,
            entity.CurrentBalance.Amount, entity.CurrentBalance.Currency,
            entity.InterestRate, entity.MinimumPayment?.Amount, entity.Notes,
            PaymentCount:    paymentCount,
            LastPaymentDate: lastPaymentDate);
    }

    public async Task DeleteAsync(Guid debtId, CancellationToken ct = default)
    {
        var userId = _currentUser.GetUserId();

        var entity = await _db.Debts
            .SingleOrDefaultAsync(d => d.Id == debtId && d.UserId == userId, ct)
            ?? throw new InvalidOperationException("Debt not found.");

        _db.Debts.Remove(entity); // Payments cascade via configuration.
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DebtPaymentDto> AddPaymentAsync(DebtPaymentCreateDto input, CancellationToken ct = default)
    {
        if (input.Amount <= 0m)
            throw new InvalidOperationException("El monto del pago debe ser mayor a 0.");

        var userId = _currentUser.GetUserId();

        // Load the debt WITHOUT including Payments — see UpdateAsync note.
        var debt = await _db.Debts
            .SingleOrDefaultAsync(d => d.Id == input.DebtId && d.UserId == userId, ct)
            ?? throw new InvalidOperationException("Debt not found.");

        var currency = debt.CurrentBalance.Currency;
        var balance  = debt.CurrentBalance.Amount;

        // Reference date for interest = last payment; fall back to debt creation.
        var lastPaymentDate = await _db.DebtPayments
            .Where(p => p.DebtId == debt.Id)
            .OrderByDescending(p => p.Date)
            .Select(p => (DateOnly?)p.Date)
            .FirstOrDefaultAsync(ct) ?? DateOnly.FromDateTime(debt.CreatedAt);

        // Interest since last payment, if the debt has an APR set.
        var interest = 0m;
        if (debt.InterestRate is > 0m and { } rate)
        {
            var days = Math.Max(0, input.Date.DayNumber - lastPaymentDate.DayNumber);
            interest = Math.Round(balance * (rate / 100m) / 365m * days, 2, MidpointRounding.AwayFromZero);
        }

        if (interest > input.Amount) interest = input.Amount;
        var principal = input.Amount - interest;

        var minimum = debt.MinimumPayment is not null
            ? Math.Min(debt.MinimumPayment.Amount, input.Amount)
            : input.Amount;
        var extra = input.Amount - minimum;

        var newBalance = Math.Max(0m, balance - principal);

        // Insert the payment via the DbSet directly — never touch a loaded
        // navigation collection on the parent while modifying the parent.
        var payment = new DebtPayment
        {
            DebtId           = debt.Id,
            Date             = input.Date,
            MinimumPayment   = new Money(minimum,  currency),
            ExtraPayment     = new Money(extra,    currency),
            InterestAmount   = new Money(interest, currency),
            PrincipalAmount  = new Money(principal, currency),
            RemainingBalance = new Money(newBalance, currency),
        };
        _db.DebtPayments.Add(payment);

        debt.CurrentBalance = new Money(newBalance, currency);

        await _db.SaveChangesAsync(ct);

        return new DebtPaymentDto(
            payment.Id, payment.Date,
            minimum, extra, interest, principal, newBalance, currency);
    }
}
