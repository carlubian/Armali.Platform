namespace Belfalas.Domain;

/// <summary>Lifecycle state of an <see cref="Era"/>.</summary>
public enum EraStatus
{
    Active,
    Archived,
}

/// <summary>
/// A bounded progression cycle (~50 weeks). An era owns its areas of focus and quest
/// design (daily habits + weekly goal pool), instances exactly one
/// <see cref="WorldTemplate"/>, and is archived as a read-only snapshot when it ends.
/// </summary>
public sealed class Era
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public int Weeks { get; set; } = 50;
    public EraStatus Status { get; set; } = EraStatus.Active;
    public required string WorldTemplateId { get; set; }

    /// <summary>
    /// Flat XP cost of one level, shared by every area of the era (areas are of equal
    /// importance). The admin calibrates the quest XP mix against this so each area can
    /// gain ~1 level/week. See <see cref="Leveling"/>.
    /// </summary>
    public int XpPerLevel { get; set; } = 100;

    public WorldTemplate? WorldTemplate { get; set; }
    public ICollection<Area> Areas { get; set; } = [];
    public ICollection<DailyHabit> DailyHabits { get; set; } = [];
    public ICollection<WeeklyGoal> WeeklyGoals { get; set; } = [];
}

/// <summary>
/// An independently progressing area of focus within an era (e.g. Work, Social). Each
/// area maps to one district of the era's world template and tracks its own XP and
/// level (0..50).
/// </summary>
public sealed class Area
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }

    /// <summary>The template district this area is bound to, once assigned at era creation.</summary>
    public Guid? DistrictId { get; set; }

    public Era? Era { get; set; }
    public District? District { get; set; }
}
