using Segaris.Api.Modules.Firebird.Domain;

namespace Segaris.UnitTests;

public sealed class FirebirdBirthdayOccurrenceTests
{
    [Fact]
    public void Occurrence_inside_a_single_year_range_is_returned_once()
    {
        var birthday = new FirebirdBirthday(6, 24);

        var occurrences = FirebirdBirthdayRules
            .OccurrencesInRange(birthday, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30))
            .ToArray();

        Assert.Equal([new DateOnly(2026, 6, 24)], occurrences);
    }

    [Fact]
    public void Occurrence_outside_the_range_is_omitted()
    {
        var birthday = new FirebirdBirthday(6, 24);

        var occurrences = FirebirdBirthdayRules
            .OccurrencesInRange(birthday, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 20))
            .ToArray();

        Assert.Empty(occurrences);
    }

    [Fact]
    public void Range_spanning_two_years_can_return_the_same_birthday_twice()
    {
        var birthday = new FirebirdBirthday(6, 24);

        var occurrences = FirebirdBirthdayRules
            .OccurrencesInRange(birthday, new DateOnly(2026, 6, 24), new DateOnly(2027, 6, 24))
            .ToArray();

        Assert.Equal([new DateOnly(2026, 6, 24), new DateOnly(2027, 6, 24)], occurrences);
    }

    [Fact]
    public void Leap_day_birthday_is_observed_on_first_of_march_in_a_non_leap_year()
    {
        var birthday = new FirebirdBirthday(2, 29);

        var nonLeap = FirebirdBirthdayRules
            .OccurrencesInRange(birthday, new DateOnly(2027, 2, 1), new DateOnly(2027, 3, 31))
            .ToArray();
        var leap = FirebirdBirthdayRules
            .OccurrencesInRange(birthday, new DateOnly(2028, 2, 1), new DateOnly(2028, 3, 31))
            .ToArray();

        Assert.Equal([new DateOnly(2027, 3, 1)], nonLeap);
        Assert.Equal([new DateOnly(2028, 2, 29)], leap);
    }
}
