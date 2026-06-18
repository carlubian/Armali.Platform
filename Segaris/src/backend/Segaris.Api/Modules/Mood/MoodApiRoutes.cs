namespace Segaris.Api.Modules.Mood;

/// <summary>Frozen route shapes for the Mood HTTP surface.</summary>
internal static class MoodApiRoutes
{
    public const string Tag = "Mood";

    /// <summary>Owner-only entry collection. Reads are bounded by an inclusive date range.</summary>
    public const string Entries = "mood/entries";

    /// <summary>Single owner-only entry by integer identifier.</summary>
    public const string EntryById = "/{entryId:int}";

    /// <summary>Strict-period dashboard aggregates for the current user.</summary>
    public const string Dashboard = "mood/dashboard";

    /// <summary>Fixed criteria enum values and derived-emotion codes for the frontend.</summary>
    public const string Options = "mood/options";

    /// <summary>Inclusive lower bound for the entry range query.</summary>
    public const string FromQuery = "from";

    /// <summary>Inclusive upper bound for the entry range query.</summary>
    public const string ToQuery = "to";

    /// <summary>Dashboard scale selector (<c>year</c>, <c>semester</c>, <c>quarter</c>, <c>month</c>).</summary>
    public const string ScaleQuery = "scale";

    /// <summary>Dashboard period token whose shape depends on the selected scale.</summary>
    public const string PeriodQuery = "period";
}
