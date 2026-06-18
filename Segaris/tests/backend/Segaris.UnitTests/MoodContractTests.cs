using Segaris.Api.Modules.Mood;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;

namespace Segaris.UnitTests;

public sealed class MoodContractTests
{
    [Fact]
    public void Criteria_vocabularies_are_frozen()
    {
        Assert.Equal(["Low", "Medium", "High"], Enum.GetNames<MoodEnergy>());
        Assert.Equal(["Negative", "Medium", "Positive"], Enum.GetNames<MoodAlignment>());
        Assert.Equal(["Harmony", "Defensive", "Offensive", "Stability"], Enum.GetNames<MoodDirection>());
        Assert.Equal(["Internal", "External"], Enum.GetNames<MoodSource>());
    }

    [Fact]
    public void Direction_uses_offensive_spelling()
    {
        Assert.Contains("Offensive", Enum.GetNames<MoodDirection>());
        Assert.DoesNotContain("Offence", Enum.GetNames<MoodDirection>());
    }

    [Fact]
    public void Criteria_catalog_matches_enum_declaration_order()
    {
        Assert.Equal(["Low", "Medium", "High"], MoodCriteriaCatalog.Energies);
        Assert.Equal(["Negative", "Medium", "Positive"], MoodCriteriaCatalog.Alignments);
        Assert.Equal(["Harmony", "Defensive", "Offensive", "Stability"], MoodCriteriaCatalog.Directions);
        Assert.Equal(["Internal", "External"], MoodCriteriaCatalog.Sources);
    }

    [Fact]
    public void Derived_emotion_combination_count_is_seventy_two()
    {
        Assert.Equal(
            72,
            MoodCriteriaCatalog.Energies.Count
                * MoodCriteriaCatalog.Alignments.Count
                * MoodCriteriaCatalog.Directions.Count
                * MoodCriteriaCatalog.Sources.Count);
        Assert.Equal(72, MoodCriteriaCatalog.DerivedEmotionCombinationCount);
    }

    [Fact]
    public void Routes_freeze_entries_dashboard_and_options()
    {
        Assert.Equal("mood/entries", MoodApiRoutes.Entries);
        Assert.Equal("/{entryId:int}", MoodApiRoutes.EntryById);
        Assert.Equal("mood/dashboard", MoodApiRoutes.Dashboard);
        Assert.Equal("mood/options", MoodApiRoutes.Options);
        Assert.Equal("from", MoodApiRoutes.FromQuery);
        Assert.Equal("to", MoodApiRoutes.ToQuery);
        Assert.Equal("scale", MoodApiRoutes.ScaleQuery);
        Assert.Equal("period", MoodApiRoutes.PeriodQuery);
        Assert.Equal("Mood", MoodApiRoutes.Tag);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("mood.entry.not_found", MoodErrorCodes.EntryNotFound.Value);
        Assert.Equal("mood.entry.validation", MoodErrorCodes.EntryValidation.Value);
        Assert.Equal("mood.range.validation", MoodErrorCodes.RangeValidation.Value);
        Assert.Equal("mood.period.validation", MoodErrorCodes.PeriodValidation.Value);
    }

    [Fact]
    public void Validation_bounds_and_timezone_are_frozen()
    {
        Assert.Equal(1, MoodDefaults.ScoreMinimum);
        Assert.Equal(5, MoodDefaults.ScoreMaximum);
        Assert.Equal(1000, MoodDefaults.NotesMaxLength);
        Assert.Equal("Europe/Madrid", MoodDefaults.HouseholdTimeZoneId);

        var newYearEve = new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 1, 1), MoodDefaults.Today(newYearEve));
    }

    [Fact]
    public void Mutation_requests_do_not_carry_a_derived_emotion()
    {
        Assert.DoesNotContain(
            typeof(CreateMoodEntryRequest).GetProperties(),
            property => property.Name is "DerivedEmotion" or "Emotion");
        Assert.DoesNotContain(
            typeof(UpdateMoodEntryRequest).GetProperties(),
            property => property.Name is "DerivedEmotion" or "Emotion");
    }

    [Fact]
    public void Entry_request_does_not_carry_visibility_or_time_of_day()
    {
        Assert.DoesNotContain(
            typeof(CreateMoodEntryRequest).GetProperties(),
            property => property.Name is "Visibility" or "Time" or "EntryTime");
    }

    [Fact]
    public void Entry_response_exposes_the_derived_emotion()
    {
        Assert.Contains(
            typeof(MoodEntryResponse).GetProperties(),
            property => property.Name == "DerivedEmotion");
    }
}
