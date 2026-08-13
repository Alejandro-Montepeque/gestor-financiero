using GestorFinanciero.Application.DTOs;
using GestorFinanciero.Domain.Enums;

namespace GestorFinanciero.UnitTests.Application;

public class BudgetItemDtoTests
{
    private static BudgetItemDto MakeItem(decimal estimated, decimal actual, CategoryType type = CategoryType.VariableExpense) =>
        new(
            CategoryId:      Guid.NewGuid(),
            CategoryName:    "Alimentos",
            CategoryColor:   "#FF0000",
            CategoryType:    type,
            EstimatedAmount: estimated,
            ActualAmount:    actual,
            Currency:        "USD");

    // ── Difference ──────────────────────────────────────────────────────

    [Fact]
    public void Difference_is_positive_when_under_budget()
    {
        var item = MakeItem(estimated: 500m, actual: 320m);

        Assert.Equal(180m, item.Difference);
    }

    [Fact]
    public void Difference_is_zero_when_actual_matches_estimated()
    {
        var item = MakeItem(estimated: 200m, actual: 200m);

        Assert.Equal(0m, item.Difference);
    }

    [Fact]
    public void Difference_is_negative_when_over_budget()
    {
        var item = MakeItem(estimated: 100m, actual: 175m);

        Assert.Equal(-75m, item.Difference);
    }

    // ── PercentUsed ─────────────────────────────────────────────────────

    [Fact]
    public void PercentUsed_returns_zero_when_estimated_is_zero_no_divby_zero()
    {
        var item = MakeItem(estimated: 0m, actual: 500m);

        Assert.Equal(0m, item.PercentUsed);
    }

    [Fact]
    public void PercentUsed_computes_ratio_times_100()
    {
        var item = MakeItem(estimated: 400m, actual: 100m);

        Assert.Equal(25m, item.PercentUsed);
    }

    [Fact]
    public void PercentUsed_can_exceed_100_when_over_budget()
    {
        var item = MakeItem(estimated: 100m, actual: 175m);

        Assert.Equal(175m, item.PercentUsed);
    }

    // ── OverBudget ──────────────────────────────────────────────────────

    [Fact]
    public void OverBudget_is_false_when_actual_below_estimated()
    {
        var item = MakeItem(estimated: 500m, actual: 320m);

        Assert.False(item.OverBudget);
    }

    [Fact]
    public void OverBudget_is_true_when_actual_exceeds_estimated()
    {
        var item = MakeItem(estimated: 100m, actual: 101m);

        Assert.True(item.OverBudget);
    }

    [Fact]
    public void OverBudget_is_false_when_estimated_is_zero_even_if_actual_positive()
    {
        // A category with no budget set isn't "over" — the user hasn't
        // planned anything for it yet, so the flag should stay false.
        var item = MakeItem(estimated: 0m, actual: 100m);

        Assert.False(item.OverBudget);
    }
}

public class BudgetDtoTests
{
    private static BudgetItemDto Item(decimal estimated, decimal actual) =>
        new(
            CategoryId:      Guid.NewGuid(),
            CategoryName:    "Cat",
            CategoryColor:   "#000",
            CategoryType:    CategoryType.VariableExpense,
            EstimatedAmount: estimated,
            ActualAmount:    actual,
            Currency:        "USD");

    [Fact]
    public void Totals_are_the_sum_of_all_items()
    {
        var dto = new BudgetDto(2026, 8, "USD", new[]
        {
            Item(500m, 320m),
            Item(200m, 250m),
            Item(100m, 0m),
        });

        Assert.Equal(800m, dto.TotalEstimated);
        Assert.Equal(570m, dto.TotalActual);
    }

    [Fact]
    public void Totals_are_zero_when_items_are_empty()
    {
        var dto = new BudgetDto(2026, 8, "USD", Array.Empty<BudgetItemDto>());

        Assert.Equal(0m, dto.TotalEstimated);
        Assert.Equal(0m, dto.TotalActual);
    }
}
