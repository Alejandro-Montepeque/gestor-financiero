using System.Globalization;

namespace GestorFinanciero.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount together with its currency code (ISO 4217).
/// Value object — immutable and compared by value.
/// </summary>
/// <remarks>
/// Arithmetic operators enforce that both operands share the same currency;
/// convert with an exchange rate before combining amounts across currencies.
/// </remarks>
public sealed record Money
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal factor)
        => new(money.Amount * factor, money.Currency);

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right) => left > right || left == right;

    public static bool operator <=(Money left, Money right) => left < right || left == right;

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Currency mismatch: cannot combine {left.Currency} with {right.Currency}");
    }

    public override string ToString()
        => Amount.ToString("N2", CultureInfo.InvariantCulture) + " " + Currency;
}
