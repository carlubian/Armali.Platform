using Belfalas.Domain;

namespace Belfalas.Tests;

public sealed class LevelingTests
{
    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(99, 100, 0)]
    [InlineData(100, 100, 1)]
    [InlineData(250, 100, 2)]
    public void LevelForXp_uses_a_flat_curve(int xp, int xpPerLevel, int expectedLevel)
    {
        Assert.Equal(expectedLevel, Leveling.LevelForXp(xp, xpPerLevel));
    }

    [Fact]
    public void LevelForXp_clamps_negative_xp_to_zero()
    {
        Assert.Equal(0, Leveling.LevelForXp(-50, 100));
    }

    [Fact]
    public void LevelForXp_never_exceeds_max_level()
    {
        var hugeXp = Leveling.XpCap(100) * 10;
        Assert.Equal(Leveling.MaxLevel, Leveling.LevelForXp(hugeXp, 100));
    }

    [Fact]
    public void XpCap_is_max_level_times_cost()
    {
        Assert.Equal(Leveling.MaxLevel * 100, Leveling.XpCap(100));
    }

    [Fact]
    public void XpIntoLevel_and_XpForNextLevel_are_complementary_below_max()
    {
        const int xp = 130;
        const int xpPerLevel = 100;

        Assert.Equal(30, Leveling.XpIntoLevel(xp, xpPerLevel));
        Assert.Equal(70, Leveling.XpForNextLevel(xp, xpPerLevel));
    }

    [Fact]
    public void At_max_level_there_is_no_progress_into_the_next()
    {
        var xp = Leveling.XpCap(100);

        Assert.Equal(0, Leveling.XpIntoLevel(xp, 100));
        Assert.Equal(0, Leveling.XpForNextLevel(xp, 100));
    }
}
