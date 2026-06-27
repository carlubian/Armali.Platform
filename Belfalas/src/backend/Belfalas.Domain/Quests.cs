namespace Belfalas.Domain;

/// <summary>
/// A recurring, habit-style daily quest defined once for the era. The daily list
/// resets each day; completing one credits its area's XP.
/// </summary>
public sealed class DailyHabit
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public Guid AreaId { get; set; }
    public required string Label { get; set; }
    public int Xp { get; set; }

    public Era? Era { get; set; }
    public Area? Area { get; set; }
}

/// <summary>
/// A larger, higher-XP weekly goal in the era's pool. The system draws (or the admin
/// overrides) the week's set from this pool; each goal is completed once per week.
/// </summary>
public sealed class WeeklyGoal
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public Guid AreaId { get; set; }
    public required string Label { get; set; }
    public int Xp { get; set; }

    public Era? Era { get; set; }
    public Area? Area { get; set; }
}
