using System.Globalization;
using Blackwing.Shared.Identity;

namespace Blackwing.UnitTests.Identity;

/// <summary>
/// <see cref="UserId"/> rejecting zero is not a nicety: the ownership filter compares against
/// <c>currentUser.UserId?.Value ?? 0</c>, so zero standing for "nobody" is only safe as long as
/// no real account can ever carry it.
/// </summary>
public sealed class UserIdTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Zero_and_negative_identifiers_are_rejected(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserId(value));

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void A_positive_identifier_keeps_its_value(int value)
    {
        var userId = new UserId(value);

        Assert.Equal(value, userId.Value);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), userId.ToString());
    }

    [Fact]
    public void Two_identifiers_with_the_same_value_are_equal()
    {
        Assert.Equal(new UserId(7), new UserId(7));
        Assert.NotEqual(new UserId(7), new UserId(8));
    }
}
