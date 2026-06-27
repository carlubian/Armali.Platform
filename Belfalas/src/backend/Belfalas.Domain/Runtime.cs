namespace Belfalas.Domain;

/// <summary>The live XP and level of one area within an era. Exactly one row per area.</summary>
public sealed class AreaProgress
{
    public Guid AreaId { get; set; }
    public Guid EraId { get; set; }
    public int Xp { get; set; }
    public int Level { get; set; }

    public Area? Area { get; set; }
    public Era? Era { get; set; }
}

/// <summary>The weekly goals drawn (or manually overridden) for a given week of an era.</summary>
public sealed class WeeklySet
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public int WeekIndex { get; set; }

    public Era? Era { get; set; }
    public ICollection<WeeklySetItem> Items { get; set; } = [];
}

/// <summary>A single weekly goal selected into a <see cref="WeeklySet"/>.</summary>
public sealed class WeeklySetItem
{
    public Guid WeeklySetId { get; set; }
    public Guid WeeklyGoalId { get; set; }

    public WeeklySet? WeeklySet { get; set; }
    public WeeklyGoal? WeeklyGoal { get; set; }
}

/// <summary>Records that a daily habit was completed on a specific date within an era.</summary>
public sealed class DailyCompletion
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public DateOnly Date { get; set; }
    public Guid DailyHabitId { get; set; }

    public Era? Era { get; set; }
    public DailyHabit? DailyHabit { get; set; }
}

/// <summary>Records that a weekly goal was completed in a specific week of an era.</summary>
public sealed class WeeklyCompletion
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public int WeekIndex { get; set; }
    public Guid WeeklyGoalId { get; set; }

    public Era? Era { get; set; }
    public WeeklyGoal? WeeklyGoal { get; set; }
}

/// <summary>
/// A built plot with its randomly-chosen variant, persisted for the lifetime of the
/// era so the world grows consistently across openings.
/// </summary>
public sealed class BuiltPlot
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public Guid DistrictId { get; set; }
    public Guid PlotId { get; set; }
    public Guid VariantId { get; set; }

    public Era? Era { get; set; }
    public Plot? Plot { get; set; }
    public Variant? Variant { get; set; }
}

/// <summary>
/// The persisted count of one denizen identity in a district. Only identity and count
/// are stored; denizens re-place randomly each time the world is opened.
/// </summary>
public sealed class DenizenCount
{
    public Guid Id { get; set; }
    public Guid EraId { get; set; }
    public Guid DistrictId { get; set; }
    public required string DenizenType { get; set; }
    public int Count { get; set; }

    public Era? Era { get; set; }
    public District? District { get; set; }
}

/// <summary>A read-only snapshot of an archived era's progress and world visual state.</summary>
public sealed class ArchivedEra
{
    public Guid EraId { get; set; }
    public DateTimeOffset ArchivedAt { get; set; }

    /// <summary>Serialized snapshot of progress + world state, produced at archival.</summary>
    public required string Snapshot { get; set; }

    public Era? Era { get; set; }
}
