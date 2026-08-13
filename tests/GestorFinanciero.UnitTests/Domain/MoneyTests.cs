using GestorFinanciero.Domain.ValueObjects;

namespace GestorFinanciero.UnitTests.Domain;

public class MoneyTests
{
    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Constructor_normalizes_currency_to_uppercase()
    {
        var money = new Money(100m, "usd");

        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_trims_whitespace_from_currency()
    {
        var money = new Money(100m, "  EUR  ");

        Assert.Equal("EUR", money.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_rejects_null_or_empty_currency(string? currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(100m, currency!));
    }

    [Fact]
    public void Zero_returns_amount_zero_in_the_requested_currency()
    {
        var zero = Money.Zero("MXN");

        Assert.Equal(0m, zero.Amount);
        Assert.Equal("MXN", zero.Currency);
    }

    // ── Equality (record semantics) ─────────────────────────────────────

    [Fact]
    public void Two_amounts_with_the_same_amount_and_currency_are_equal()
    {
        var a = new Money(50.25m, "USD");
        var b = new Money(50.25m, "usd");   // different case, same normalized value

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Different_currencies_are_not_equal_even_with_same_amount()
    {
        var usd = new Money(100m, "USD");
        var eur = new Money(100m, "EUR");

        Assert.NotEqual(usd, eur);
    }

    // ── Arithmetic ──────────────────────────────────────────────────────

    [Fact]
    public void Addition_of_same_currency_produces_correct_total()
    {
        var total = new Money(100m, "USD") + new Money(25.50m, "USD");

        Assert.Equal(125.50m, total.Amount);
        Assert.Equal("USD", total.Currency);
    }

    [Fact]
    public void Subtraction_of_same_currency_can_go_negative()
    {
        var result = new Money(50m, "USD") - new Money(75m, "USD");

        Assert.Equal(-25m, result.Amount);
    }

    [Fact]
    public void Multiplication_by_a_scalar_keeps_the_currency()
    {
        var total = new Money(10m, "USD") * 3m;

        Assert.Equal(30m, total.Amount);
        Assert.Equal("USD", total.Currency);
    }

    [Fact]
    public void Addition_across_currencies_throws()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        var ex = Assert.Throws<InvalidOperationException>(() => { var _ = usd + eur; });
        Assert.Contains("USD", ex.Message);
        Assert.Contains("EUR", ex.Message);
    }

    [Fact]
    public void Subtraction_across_currencies_throws()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.Throws<InvalidOperationException>(() => { var _ = usd - eur; });
    }

    // ── Comparison ──────────────────────────────────────────────────────

    [Fact]
    public void Comparison_operators_work_within_the_same_currency()
    {
        var small = new Money(50m, "USD");
        var big   = new Money(100m, "USD");

        Assert.True(big > small);
        Assert.True(small < big);
        Assert.True(big >= new Money(100m, "USD"));
        Assert.True(small <= new Money(50m, "USD"));
    }

    [Fact]
    public void Comparison_across_currencies_throws()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.Throws<InvalidOperationException>(() => { var _ = usd > eur; });
    }

    // ── Formatting ──────────────────────────────────────────────────────

    [Fact]
    public void ToString_formats_with_two_decimals_and_invariant_culture()
    {
        // Even if the current culture uses a decimal comma, ToString must use a dot.
        var money = new Money(1234.5m, "USD");

        Assert.Equal("1,234.50 USD", money.ToString());
    }
}
