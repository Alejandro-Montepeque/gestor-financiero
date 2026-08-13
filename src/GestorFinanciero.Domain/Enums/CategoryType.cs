namespace GestorFinanciero.Domain.Enums;

/// <summary>
/// Classification for financial categories. Preserves the taxonomy from the
/// legacy version (INGRESO, AHORRO, GASTO_FIJO, GASTO_VARIABLE, DEUDA).
/// </summary>
/// <remarks>
/// Explicit integer values are set so the database persists a stable code even
/// if the enum is reordered in a future refactor.
/// </remarks>
public enum CategoryType
{
    Income = 1,
    Savings = 2,
    FixedExpense = 3,
    VariableExpense = 4,
    Debt = 5
}
