using System.Text.Json;
using Segaris.Api.Modules.Games;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class GamesContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Platform_status_and_colour_enum_values_are_frozen()
    {
        Assert.Equal(
            ["PC", "Console", "Mobile", "BoardGame", "TabletopRpg", "Other"],
            Enum.GetNames<GamePlatform>());
        Assert.Equal(
            [0, 1, 2, 3, 4, 5],
            Enum.GetValues<GamePlatform>().Select(value => (int)value).ToArray());

        Assert.Equal(["Planning", "Active", "Completed"], Enum.GetNames<PlaythroughStatus>());
        Assert.Equal([0, 1, 2], Enum.GetValues<PlaythroughStatus>().Select(value => (int)value).ToArray());

        Assert.Equal(
            ["Blue", "Green", "Amber", "Red", "Purple", "Pink", "Teal", "Indigo", "Slate", "Orange"],
            Enum.GetNames<SectionColor>());
    }

    [Fact]
    public void Creation_defaults_and_validation_bounds_are_frozen()
    {
        Assert.Equal(RecordVisibility.Public, GamesDefaults.Visibility);
        Assert.Equal(PlaythroughStatus.Planning, GamesDefaults.Status);
        Assert.False(GamesDefaults.GoalCompleted);
        Assert.Equal(200, GamesDefaults.NameMaximumLength);
        Assert.Equal(500, GamesDefaults.GoalTextMaximumLength);
        Assert.Equal(100, GamesDefaults.TagMaximumLength);
        Assert.Equal((1, 12), (GamesDefaults.MinimumStartMonth, GamesDefaults.MaximumStartMonth));
        Assert.Equal((1, 9999), (GamesDefaults.MinimumStartYear, GamesDefaults.MaximumStartYear));
    }

    [Fact]
    public void Routes_freeze_games_playthroughs_sections_and_goals()
    {
        Assert.Equal("games", GamesApiRoutes.Games);
        Assert.Equal("games/games", GamesApiRoutes.GameCatalogue);
        Assert.Equal("/{gameId:int}", GamesApiRoutes.GameById);
        Assert.Equal("/{gameId:int}/move", GamesApiRoutes.GameMove);
        Assert.Equal("/{gameId:int}/deletion-impact", GamesApiRoutes.GameDeletionImpact);
        Assert.Equal("/{gameId:int}/replace-and-delete", GamesApiRoutes.GameReplaceAndDelete);
        Assert.Equal("games/playthroughs", GamesApiRoutes.Playthroughs);
        Assert.Equal("/{playthroughId:int}", GamesApiRoutes.PlaythroughById);
        Assert.Equal("/{playthroughId:int}/sections", GamesApiRoutes.Sections);
        Assert.Equal("/{playthroughId:int}/sections/order", GamesApiRoutes.SectionsOrder);
        Assert.Equal("/{playthroughId:int}/sections/{sectionId:int}", GamesApiRoutes.SectionById);
        Assert.Equal("/{playthroughId:int}/sections/{sectionId:int}/goals", GamesApiRoutes.Goals);
        Assert.Equal(
            "/{playthroughId:int}/sections/{sectionId:int}/goals/{goalId:int}",
            GamesApiRoutes.GoalById);
        Assert.Equal(
            "/{playthroughId:int}/sections/{sectionId:int}/goals/{goalId:int}/completion",
            GamesApiRoutes.GoalCompletion);
    }

    [Fact]
    public void Playthrough_query_contract_is_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "name", "game", "startDate", "status", "progress", "id" },
            PlaythroughQuery.AllowedSortFields);
        Assert.Equal("name", PlaythroughQuery.SortFields.Default);
        Assert.Equal("id", PlaythroughQuery.SortFields.TieBreaker);
        Assert.Equal("asc", PlaythroughQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], PlaythroughQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_sort_is_name_ascending_with_identifier_tie_breaker()
    {
        var sort = SortRequest.Create(
            null,
            null,
            PlaythroughQuery.AllowedSortFields,
            PlaythroughQuery.SortFields.Default,
            PlaythroughQuery.SortFields.TieBreaker);

        Assert.Equal(("name", SortDirection.Ascending, "id"), (sort.Field, sort.Direction, sort.TieBreakerField));
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(GamesConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [GamesCatalogKind.Games],
            GamesConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        // The game catalogue is optional (may be empty) and referenced games may
        // only be replaced, never cleared.
        Assert.Equal(("games", false, false), (
            GamesConfigurationContracts.OwnedCatalogs[0].RouteSegment,
            GamesConfigurationContracts.OwnedCatalogs[0].IsRequired,
            GamesConfigurationContracts.OwnedCatalogs[0].SupportsClearing));
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("games.game.not_found", GamesErrorCodes.GameNotFound.Value);
        Assert.Equal("games.game.duplicate_name", GamesErrorCodes.GameDuplicateName.Value);
        Assert.Equal("games.game.referenced", GamesErrorCodes.GameReferenced.Value);
        Assert.Equal("games.game.invalid_replacement", GamesErrorCodes.GameInvalidReplacement.Value);
        Assert.Equal("games.playthrough.not_found", GamesErrorCodes.PlaythroughNotFound.Value);
        Assert.Equal("games.playthrough.validation", GamesErrorCodes.PlaythroughValidation.Value);
        Assert.Equal("games.playthrough.unknown_game", GamesErrorCodes.UnknownGameReference.Value);
        Assert.Equal("games.playthrough.visibility_forbidden", GamesErrorCodes.PlaythroughVisibilityForbidden.Value);
        Assert.Equal("games.section.not_found", GamesErrorCodes.SectionNotFound.Value);
        Assert.Equal("games.section.duplicate_name", GamesErrorCodes.SectionDuplicateName.Value);
        Assert.Equal("games.section.invalid_order", GamesErrorCodes.SectionInvalidOrder.Value);
        Assert.Equal("games.goal.not_found", GamesErrorCodes.GoalNotFound.Value);
        Assert.Equal("games.goal.validation", GamesErrorCodes.GoalValidation.Value);
    }

    [Fact]
    public void Tag_normalization_trims_drops_empties_and_dedupes_case_insensitively()
    {
        var normalized = GamesTagNormalization.Normalize(
            [" Story ", "story", "STORY", "", "   ", null, "Co-op", "co-op "]);

        // The first kept value's capitalization is preserved; later case-insensitive
        // duplicates are discarded, and ordering follows first appearance.
        Assert.Equal(["Story", "Co-op"], normalized);
    }

    [Fact]
    public void Tag_normalization_of_null_is_empty()
    {
        Assert.Empty(GamesTagNormalization.Normalize(null));
    }

    [Fact]
    public void Requests_serialize_to_the_frozen_wire_shape()
    {
        var playthrough = new CreatePlaythroughRequest("Ironman", 3, 2026, 7, "Active", ["Story", "Hard"], "Private");
        var goal = new GoalCompletionRequest(true);

        using var playthroughDocument = JsonDocument.Parse(JsonSerializer.Serialize(playthrough, Web));
        using var goalDocument = JsonDocument.Parse(JsonSerializer.Serialize(goal, Web));

        Assert.Equal(3, playthroughDocument.RootElement.GetProperty("gameId").GetInt32());
        Assert.Equal(2026, playthroughDocument.RootElement.GetProperty("startYear").GetInt32());
        Assert.Equal(7, playthroughDocument.RootElement.GetProperty("startMonth").GetInt32());
        Assert.Equal("Active", playthroughDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, playthroughDocument.RootElement.GetProperty("tags").GetArrayLength());
        Assert.True(goalDocument.RootElement.GetProperty("completed").GetBoolean());
    }
}
