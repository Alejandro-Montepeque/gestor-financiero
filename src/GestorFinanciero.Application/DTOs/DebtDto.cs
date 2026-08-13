namespace GestorFinanciero.Application.DTOs;

/// <summary>List-view projection: enough to show a debt on the /debts grid.</summary>
public sealed record DebtDto(
    Guid Id,
    string Name,
    decimal CurrentBalance,
    string Currency,
    decimal? InterestRate,
    decimal? MinimumPayment,
    string? Notes,
    int PaymentCount,
    DateOnly? LastPaymentDate);

/// <summary>Detail view: debt + full payment history in date order.</summary>
public sealed record DebtDetailDto(
    DebtDto Debt,
    IReadOnlyList<DebtPaymentDto> Payments);

/// <summary>One row of the payment history.</summary>
public sealed record DebtPaymentDto(
    Guid Id,
    DateOnly Date,
    decimal MinimumPayment,
    decimal ExtraPayment,
    decimal InterestAmount,
    decimal PrincipalAmount,
    decimal? RemainingBalance,
    string Currency)
{
    public decimal TotalPaid => MinimumPayment + ExtraPayment;
}

public sealed class DebtUpsertDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal? InterestRate { get; set; }
    public decimal? MinimumPayment { get; set; }
    public string? Notes { get; set; }
}

public sealed class DebtPaymentCreateDto
{
    public Guid DebtId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
}
