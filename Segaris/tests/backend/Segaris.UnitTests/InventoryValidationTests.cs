using Segaris.Api.Modules.Inventory.Domain;

namespace Segaris.UnitTests;

public sealed class InventoryValidationTests
{
    [Theory]
    [InlineData("Olive oil", "OLIVE OIL")]
    [InlineData(" olive oil ", "OLIVE OIL")]
    [InlineData("Olive  Oil", "OLIVE  OIL")]
    public void Item_name_normalization_trims_and_preserves_internal_spaces(
        string value,
        string expected)
    {
        Assert.Equal(expected, InventoryValidation.NormalizeItemName(value));
    }

    [Fact]
    public void Item_name_validation_preserves_display_capitalization()
    {
        Assert.Equal("Olive Oil", InventoryValidation.ValidateItemName(" Olive Oil "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Item_name_validation_rejects_blank_values(string value)
    {
        Assert.Throws<InventoryValidationException>(() => InventoryValidation.ValidateItemName(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12.34)]
    public void Stock_accepts_nonnegative_values_with_two_decimals(decimal value)
    {
        Assert.Equal(value, InventoryValidation.ValidateStock(value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void Stock_rejects_negative_or_overprecise_values(decimal value)
    {
        Assert.Throws<InventoryValidationException>(() => InventoryValidation.ValidateStock(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(1.001)]
    public void Positive_quantity_rejects_zero_negative_or_overprecise_values(decimal value)
    {
        Assert.Throws<InventoryValidationException>(
            () => InventoryValidation.ValidatePositiveQuantity(value));
    }

    [Fact]
    public void Stock_increase_adds_to_current_stock()
    {
        Assert.Equal(
            7.50m,
            InventoryValidation.ApplyStockAdjustment(
                5m, InventoryStockAdjustmentDirection.Increase, 2.50m));
    }

    [Fact]
    public void Stock_decrease_subtracts_from_current_stock()
    {
        Assert.Equal(
            2.50m,
            InventoryValidation.ApplyStockAdjustment(
                5m, InventoryStockAdjustmentDirection.Decrease, 2.50m));
    }

    [Fact]
    public void Stock_decrease_rejecting_a_negative_result()
    {
        Assert.Throws<InventoryValidationException>(
            () => InventoryValidation.ApplyStockAdjustment(
                1m, InventoryStockAdjustmentDirection.Decrease, 2m));
    }

    [Fact]
    public void Notes_preserve_content_within_the_limit()
    {
        Assert.Equal(" Notes ", InventoryValidation.ValidateNotes(" Notes "));
        Assert.Null(InventoryValidation.ValidateNotes(""));
        Assert.Throws<InventoryValidationException>(
            () => InventoryValidation.ValidateNotes(new string('x', 4001)));
    }
}
