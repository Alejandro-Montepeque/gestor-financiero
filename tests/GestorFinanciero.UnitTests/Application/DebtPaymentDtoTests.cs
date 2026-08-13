using GestorFinanciero.Application.DTOs;

namespace GestorFinanciero.UnitTests.Application;

public class DebtPaymentDtoTests
{
    private static DebtPaymentDto MakePayment(decimal minimum, decimal extra) =>
        new(
            Id:               Guid.NewGuid(),
            Date:             new DateOnly(2026, 8, 13),
            MinimumPayment:   minimum,
            ExtraPayment:     extra,
            InterestAmount:   0m,
            PrincipalAmount:  minimum + extra,
            RemainingBalance: 100m,
            Currency:         "USD");

    [Fact]
    public void TotalPaid_is_minimum_plus_extra()
    {
        var payment = MakePayment(minimum: 100m, extra: 25m);

        Assert.Equal(125m, payment.TotalPaid);
    }

    [Fact]
    public void TotalPaid_equals_minimum_when_no_extra()
    {
        var payment = MakePayment(minimum: 50m, extra: 0m);

        Assert.Equal(50m, payment.TotalPaid);
    }

    [Fact]
    public void TotalPaid_is_zero_when_both_components_zero()
    {
        var payment = MakePayment(minimum: 0m, extra: 0m);

        Assert.Equal(0m, payment.TotalPaid);
    }
}
