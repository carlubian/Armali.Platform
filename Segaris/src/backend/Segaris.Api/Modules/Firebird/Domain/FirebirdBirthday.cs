namespace Segaris.Api.Modules.Firebird.Domain;

internal readonly record struct FirebirdBirthday(int Month, int Day);

internal static class FirebirdBirthdayRules
{
    public static FirebirdBirthday? Create(int? month, int? day)
    {
        if (month is null && day is null)
        {
            return null;
        }

        if (month is null || day is null)
        {
            throw new ArgumentException("A Firebird birthday must include both month and day, or neither.");
        }

        if (!IsValid(month.Value, day.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(day), "The birthday month/day pair is not valid.");
        }

        return new FirebirdBirthday(month.Value, day.Value);
    }

    public static bool IsValid(int month, int day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DaysInMonth(month);

    public static int CompareCalendar(FirebirdBirthday? left, FirebirdBirthday? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return 1;
        }

        if (right is null)
        {
            return -1;
        }

        var monthComparison = left.Value.Month.CompareTo(right.Value.Month);
        return monthComparison != 0
            ? monthComparison
            : left.Value.Day.CompareTo(right.Value.Day);
    }

    public static DateOnly NextOccurrence(FirebirdBirthday birthday, DateOnly today)
    {
        var occurrence = ObservedDateInYear(birthday, today.Year);
        return occurrence < today
            ? ObservedDateInYear(birthday, today.Year + 1)
            : occurrence;
    }

    /// <summary>
    /// Yields every observed birthday occurrence that falls within the inclusive
    /// <paramref name="from"/>/<paramref name="to"/> civil-date range, applying the
    /// source-owned leap-day rule. A range may span two civil years, so a birthday can
    /// appear at most twice (for example a 29 February birthday observed on 1 March in
    /// a non-leap year). Occurrences are returned in ascending date order.
    /// </summary>
    public static IEnumerable<DateOnly> OccurrencesInRange(FirebirdBirthday birthday, DateOnly from, DateOnly to)
    {
        for (var year = from.Year; year <= to.Year; year++)
        {
            var occurrence = ObservedDateInYear(birthday, year);
            if (occurrence >= from && occurrence <= to)
            {
                yield return occurrence;
            }
        }
    }

    private static int DaysInMonth(int month) =>
        month switch
        {
            2 => 29,
            4 or 6 or 9 or 11 => 30,
            _ => 31,
        };

    private static DateOnly ObservedDateInYear(FirebirdBirthday birthday, int year)
    {
        if (birthday is { Month: 2, Day: 29 } && !DateTime.IsLeapYear(year))
        {
            return new DateOnly(year, 3, 1);
        }

        return new DateOnly(year, birthday.Month, birthday.Day);
    }
}
