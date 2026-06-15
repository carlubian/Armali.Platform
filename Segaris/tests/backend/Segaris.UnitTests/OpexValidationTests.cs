using Segaris.Api.Modules.Opex.Domain;

namespace Segaris.UnitTests;

public sealed class OpexValidationTests
{
    [Theory]
    [InlineData("Netflix", "NETFLIX")]
    [InlineData(" netflix ", "NETFLIX")]
    [InlineData("Netflix Premium", "NETFLIX PREMIUM")]
    [InlineData("Netflix  Premium", "NETFLIX  PREMIUM")]
    public void Contract_name_normalization_trims_and_preserves_internal_spaces(
        string value,
        string expected)
    {
        Assert.Equal(expected, OpexValidation.NormalizeContractName(value));
    }

    [Fact]
    public void Contract_name_validation_preserves_display_capitalization()
    {
        Assert.Equal("NetFlix", OpexValidation.ValidateContractName(" NetFlix "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12.34)]
    public void User_amount_accepts_nonnegative_values_with_two_decimals(decimal value)
    {
        Assert.Equal(value, OpexValidation.ValidateUserAmount(value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public void User_amount_rejects_negative_or_overprecise_values(decimal value)
    {
        Assert.Throws<OpexValidationException>(() => OpexValidation.ValidateUserAmount(value));
    }

    [Fact]
    public void Currency_conversion_rounds_away_from_zero()
    {
        Assert.Equal(1.01m, OpexValidation.ConvertAmount(1m, 1.005m));
        Assert.Equal(0m, OpexValidation.ConvertAmount(0m, 1.005m));
    }

    [Fact]
    public void Description_is_trimmed_but_notes_preserve_content()
    {
        Assert.Equal("Invoice", OpexValidation.ValidateDescription(" Invoice "));
        Assert.Equal(" Notes ", OpexValidation.ValidateNotes(" Notes "));
    }
}
