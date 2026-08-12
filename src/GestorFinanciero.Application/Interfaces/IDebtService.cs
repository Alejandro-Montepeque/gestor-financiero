using GestorFinanciero.Application.DTOs;

namespace GestorFinanciero.Application.Interfaces;

/// <summary>
/// CRUD for the user's outstanding debts plus payment tracking with
/// automatic interest / principal split when an interest rate is set.
/// </summary>
public interface IDebtService
{
    Task<IReadOnlyList<DebtDto>> GetAllAsync(CancellationToken ct = default);

    Task<DebtDetailDto?> GetByIdAsync(Guid debtId, CancellationToken ct = default);

    Task<DebtDto> CreateAsync(DebtUpsertDto input, CancellationToken ct = default);

    Task<DebtDto> UpdateAsync(DebtUpsertDto input, CancellationToken ct = default);

    Task DeleteAsync(Guid debtId, CancellationToken ct = default);

    /// <summary>
    /// Records a payment. If the debt has an <c>InterestRate</c>, we split the
    /// payment into interest (based on the balance since the last payment) and
    /// principal. Otherwise the whole payment reduces the principal.
    /// </summary>
    Task<DebtPaymentDto> AddPaymentAsync(DebtPaymentCreateDto input, CancellationToken ct = default);
}
