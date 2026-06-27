namespace Belfalas.Domain;

/// <summary>
/// Pure leveling math for the progression engine. The curve is flat: every level within
/// an area costs the same <c>xpPerLevel</c> (see <see cref="Era.XpPerLevel"/>), and an
/// area progresses 0..<see cref="MaxLevel"/>. Kept free of persistence so it can be
/// reasoned about and unit-tested in isolation.
/// </summary>
public static class Leveling
{
    /// <summary>The highest level an area can reach; at this level the district is complete.</summary>
    public const int MaxLevel = 50;

    /// <summary>The level an area is at for a given total XP, clamped to [0, <see cref="MaxLevel"/>].</summary>
    public static int LevelForXp(int xp, int xpPerLevel) =>
        Math.Min(MaxLevel, Math.Max(0, xp) / xpPerLevel);

    /// <summary>The XP total that corresponds to reaching <see cref="MaxLevel"/>; XP is clamped here so a completed area never accrues further.</summary>
    public static int XpCap(int xpPerLevel) => MaxLevel * xpPerLevel;

    /// <summary>XP earned into the current level (0..<c>xpPerLevel</c>, and 0 once at <see cref="MaxLevel"/>).</summary>
    public static int XpIntoLevel(int xp, int xpPerLevel) =>
        LevelForXp(xp, xpPerLevel) >= MaxLevel ? 0 : Math.Max(0, xp) % xpPerLevel;

    /// <summary>XP still required to reach the next level (0 once at <see cref="MaxLevel"/>).</summary>
    public static int XpForNextLevel(int xp, int xpPerLevel) =>
        LevelForXp(xp, xpPerLevel) >= MaxLevel ? 0 : xpPerLevel - XpIntoLevel(xp, xpPerLevel);
}
