using System.Globalization;

namespace Segaris.Api.Modules.Mood.Domain;

/// <summary>Supported dashboard time scales, in default-selection order.</summary>
internal enum MoodDashboardScale
{
    Year,
    Semester,
    Quarter,
    Month,
}

/// <summary>
/// A strict calendar period for the dashboard, identified by a compact token whose
/// shape depends on the scale:
/// <list type="bullet">
/// <item><description>Year: <c>2026</c></description></item>
/// <item><description>Semester: <c>2026-S1</c>, <c>2026-S2</c></description></item>
/// <item><description>Quarter: <c>2026-Q1</c> .. <c>2026-Q4</c></description></item>
/// <item><description>Month: <c>2026-01</c> .. <c>2026-12</c></description></item>
/// </list>
/// Boundaries are inclusive civil dates. <see cref="Index"/> is the 1-based slot
/// within the year (always 1 for <see cref="MoodDashboardScale.Year"/>).
/// </summary>
internal readonly record struct MoodPeriod
{
    /// <summary>Earliest civil year a period token may reference.</summary>
    public const int MinYear = 1;

    /// <summary>Latest civil year a period token may reference.</summary>
    public const int MaxYear = 9999;

    private MoodPeriod(MoodDashboardScale scale, int year, int index)
    {
        Scale = scale;
        Year = year;
        Index = index;
    }

    public MoodDashboardScale Scale { get; }

    public int Year { get; }

    public int Index { get; }

    /// <summary>Inclusive first civil date of the period.</summary>
    public DateOnly Start => Scale switch
    {
        MoodDashboardScale.Year => new DateOnly(Year, 1, 1),
        MoodDashboardScale.Semester => new DateOnly(Year, (Index - 1) * 6 + 1, 1),
        MoodDashboardScale.Quarter => new DateOnly(Year, (Index - 1) * 3 + 1, 1),
        MoodDashboardScale.Month => new DateOnly(Year, Index, 1),
        _ => throw new InvalidOperationException(),
    };

    /// <summary>Inclusive last civil date of the period.</summary>
    public DateOnly End => Scale switch
    {
        MoodDashboardScale.Year => new DateOnly(Year, 12, 31),
        MoodDashboardScale.Semester => new DateOnly(Year, Index * 6, DateTime.DaysInMonth(Year, Index * 6)),
        MoodDashboardScale.Quarter => new DateOnly(Year, Index * 3, DateTime.DaysInMonth(Year, Index * 3)),
        MoodDashboardScale.Month => new DateOnly(Year, Index, DateTime.DaysInMonth(Year, Index)),
        _ => throw new InvalidOperationException(),
    };

    /// <summary>The compact period token for this scale.</summary>
    public string Token => Scale switch
    {
        MoodDashboardScale.Year => Year.ToString("D4", CultureInfo.InvariantCulture),
        MoodDashboardScale.Semester => $"{Year:D4}-S{Index}",
        MoodDashboardScale.Quarter => $"{Year:D4}-Q{Index}",
        MoodDashboardScale.Month => $"{Year:D4}-{Index:D2}",
        _ => throw new InvalidOperationException(),
    };

    private int SlotsPerYear => Scale switch
    {
        MoodDashboardScale.Year => 1,
        MoodDashboardScale.Semester => 2,
        MoodDashboardScale.Quarter => 4,
        MoodDashboardScale.Month => 12,
        _ => throw new InvalidOperationException(),
    };

    /// <summary>The same scale's period immediately before this one, rolling over years.</summary>
    public MoodPeriod Previous => Shift(-1);

    /// <summary>The same scale's period immediately after this one, rolling over years.</summary>
    public MoodPeriod Next => Shift(1);

    /// <summary>The strict period of the given scale that contains <paramref name="today"/>.</summary>
    public static MoodPeriod Current(MoodDashboardScale scale, DateOnly today) => scale switch
    {
        MoodDashboardScale.Year => new MoodPeriod(scale, today.Year, 1),
        MoodDashboardScale.Semester => new MoodPeriod(scale, today.Year, (today.Month - 1) / 6 + 1),
        MoodDashboardScale.Quarter => new MoodPeriod(scale, today.Year, (today.Month - 1) / 3 + 1),
        MoodDashboardScale.Month => new MoodPeriod(scale, today.Year, today.Month),
        _ => throw new ArgumentOutOfRangeException(nameof(scale)),
    };

    /// <summary>Parses a scale selector such as <c>year</c> or <c>Quarter</c> (case-insensitive).</summary>
    public static bool TryParseScale(string? value, out MoodDashboardScale scale)
    {
        scale = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out scale)
            && Enum.IsDefined(scale);
    }

    /// <summary>Parses a compact period token for the given scale.</summary>
    public static bool TryParse(MoodDashboardScale scale, string? token, out MoodPeriod period)
    {
        period = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        token = token.Trim();

        return scale switch
        {
            MoodDashboardScale.Year => TryParseYear(token, out period),
            MoodDashboardScale.Semester => TryParseIndexed(scale, token, 'S', 2, out period),
            MoodDashboardScale.Quarter => TryParseIndexed(scale, token, 'Q', 4, out period),
            MoodDashboardScale.Month => TryParseMonth(token, out period),
            _ => false,
        };
    }

    private MoodPeriod Shift(int delta)
    {
        var slots = SlotsPerYear;
        var zeroBased = (Index - 1) + delta;
        var yearDelta = (int)Math.Floor(zeroBased / (double)slots);
        var index = zeroBased - (yearDelta * slots) + 1;
        return new MoodPeriod(Scale, Year + yearDelta, index);
    }

    private static bool TryParseYear(string token, out MoodPeriod period)
    {
        period = default;
        if (!TryParseYearComponent(token, out var year))
        {
            return false;
        }

        period = new MoodPeriod(MoodDashboardScale.Year, year, 1);
        return true;
    }

    private static bool TryParseIndexed(
        MoodDashboardScale scale,
        string token,
        char marker,
        int maxIndex,
        out MoodPeriod period)
    {
        period = default;

        // Expected shape: yyyy-<marker>n, for example 2026-S1.
        if (token.Length != 7 || token[4] != '-' || char.ToUpperInvariant(token[5]) != marker)
        {
            return false;
        }

        if (!TryParseYearComponent(token[..4], out var year))
        {
            return false;
        }

        if (!int.TryParse(
                token.AsSpan(6, 1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var index)
            || index < 1
            || index > maxIndex)
        {
            return false;
        }

        period = new MoodPeriod(scale, year, index);
        return true;
    }

    private static bool TryParseMonth(string token, out MoodPeriod period)
    {
        period = default;

        // Expected shape: yyyy-MM, for example 2026-03.
        if (token.Length != 7 || token[4] != '-')
        {
            return false;
        }

        if (!TryParseYearComponent(token[..4], out var year))
        {
            return false;
        }

        if (!int.TryParse(
                token.AsSpan(5, 2),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var month)
            || month < 1
            || month > 12)
        {
            return false;
        }

        period = new MoodPeriod(MoodDashboardScale.Month, year, month);
        return true;
    }

    private static bool TryParseYearComponent(string token, out int year)
    {
        year = 0;
        if (token.Length != 4
            || !int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out year))
        {
            return false;
        }

        return year is >= MinYear and <= MaxYear;
    }
}
