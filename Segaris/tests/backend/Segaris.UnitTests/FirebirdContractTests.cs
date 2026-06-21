using System.Text.Json;
using Segaris.Api.Modules.Firebird;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class FirebirdContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Fixed_vocabularies_are_frozen()
    {
        Assert.Equal(["Unknown", "Active", "Unavailable", "Blocked"], Enum.GetNames<PersonStatus>());
        Assert.Equal(["Public", "Private"], Enum.GetNames<RecordVisibility>());
    }

    [Fact]
    public void Creation_defaults_and_limits_are_frozen()
    {
        Assert.Equal(PersonStatus.Unknown, FirebirdDefaults.Status);
        Assert.Equal(RecordVisibility.Public, FirebirdDefaults.Visibility);
        Assert.Equal(200, FirebirdDefaults.NameMaximumLength);
        Assert.Equal(2000, FirebirdDefaults.NotesMaximumLength);
        Assert.Equal(200, FirebirdDefaults.UsernameHandleMaximumLength);
        Assert.Equal(1000, FirebirdDefaults.UsernameNotesMaximumLength);
        Assert.Equal(2000, FirebirdDefaults.InteractionDescriptionMaximumLength);
        Assert.Equal(200, FirebirdDefaults.CatalogNameMaximumLength);
        Assert.Equal(7, FirebirdDefaults.AttentionWindowDays);
    }

    [Fact]
    public void Routes_freeze_person_avatar_username_interaction_and_catalogue_shapes()
    {
        Assert.Equal("people", FirebirdApiRoutes.People);
        Assert.Equal("/{personId:int}", FirebirdApiRoutes.PersonById);
        Assert.Equal("/{personId:int}/avatar", FirebirdApiRoutes.PersonAvatar);
        Assert.Equal("/{personId:int}/usernames", FirebirdApiRoutes.PersonUsernames);
        Assert.Equal("/{personId:int}/usernames/{usernameId:int}", FirebirdApiRoutes.PersonUsernameById);
        Assert.Equal("/{personId:int}/interactions", FirebirdApiRoutes.PersonInteractions);
        Assert.Equal("/{personId:int}/interactions/{interactionId:int}", FirebirdApiRoutes.PersonInteractionById);
        Assert.Equal("people/categories", FirebirdApiRoutes.Categories);
        Assert.Equal("people/platforms", FirebirdApiRoutes.Platforms);
    }

    [Fact]
    public void Person_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "category", "status", "birthday", "visibility", "id",
            },
            FirebirdPeopleQuery.AllowedSortFields);
        Assert.Equal("name", FirebirdPeopleQuery.SortFields.Default);
        Assert.Equal("id", FirebirdPeopleQuery.SortFields.TieBreaker);
        Assert.Equal("asc", FirebirdPeopleQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], FirebirdPeopleQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_person_sort_is_name_ascending()
    {
        var sort = SortRequest.Create(
            null,
            null,
            FirebirdPeopleQuery.AllowedSortFields,
            FirebirdPeopleQuery.SortFields.Default,
            FirebirdPeopleQuery.SortFields.TieBreaker);

        Assert.Equal("name", sort.Field);
        Assert.Equal(SortDirection.Ascending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Pagination_rejects_page_sizes_outside_platform_bounds(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(1, pageSize));
    }

    [Fact]
    public void Birthday_is_all_or_nothing_and_allows_february_twenty_nine()
    {
        Assert.Null(FirebirdBirthdayRules.Create(null, null));
        Assert.Equal(new FirebirdBirthday(2, 29), FirebirdBirthdayRules.Create(2, 29));

        Assert.Throws<ArgumentException>(() => FirebirdBirthdayRules.Create(2, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => FirebirdBirthdayRules.Create(2, 30));
        Assert.Throws<ArgumentOutOfRangeException>(() => FirebirdBirthdayRules.Create(13, 1));
    }

    [Fact]
    public void Birthday_calendar_order_sorts_nulls_last()
    {
        FirebirdBirthday? january = new FirebirdBirthday(1, 31);
        FirebirdBirthday? february = new FirebirdBirthday(2, 1);
        FirebirdBirthday? none = null;

        Assert.True(FirebirdBirthdayRules.CompareCalendar(january, february) < 0);
        Assert.True(FirebirdBirthdayRules.CompareCalendar(february, january) > 0);
        Assert.True(FirebirdBirthdayRules.CompareCalendar(none, february) > 0);
        Assert.Equal(0, FirebirdBirthdayRules.CompareCalendar(none, null));
    }

    [Fact]
    public void Birthday_next_occurrence_wraps_year_and_observes_leap_day_on_march_first()
    {
        Assert.Equal(
            new DateOnly(2026, 6, 21),
            FirebirdBirthdayRules.NextOccurrence(new FirebirdBirthday(6, 21), new DateOnly(2026, 6, 21)));
        Assert.Equal(
            new DateOnly(2027, 1, 3),
            FirebirdBirthdayRules.NextOccurrence(new FirebirdBirthday(1, 3), new DateOnly(2026, 12, 28)));
        Assert.Equal(
            new DateOnly(2026, 3, 1),
            FirebirdBirthdayRules.NextOccurrence(new FirebirdBirthday(2, 29), new DateOnly(2026, 2, 28)));
        Assert.Equal(
            new DateOnly(2028, 2, 29),
            FirebirdBirthdayRules.NextOccurrence(new FirebirdBirthday(2, 29), new DateOnly(2028, 2, 28)));
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(FirebirdConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [FirebirdCatalogKind.PersonCategories, FirebirdCatalogKind.UsernamePlatforms],
            FirebirdConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        var categories = FirebirdConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("categories", categories.RouteSegment);
        Assert.True(categories.IsRequired);
        Assert.False(categories.SupportsClearing);

        var platforms = FirebirdConfigurationContracts.OwnedCatalogs[1];
        Assert.Equal("platforms", platforms.RouteSegment);
        Assert.True(platforms.IsRequired);
        Assert.False(platforms.SupportsClearing);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("firebird.person.not_found", FirebirdErrorCodes.PersonNotFound.Value);
        Assert.Equal("firebird.person.validation", FirebirdErrorCodes.PersonValidation.Value);
        Assert.Equal("firebird.person.visibility_forbidden", FirebirdErrorCodes.PersonVisibilityForbidden.Value);
        Assert.Equal("firebird.avatar.not_found", FirebirdErrorCodes.AvatarNotFound.Value);
        Assert.Equal("firebird.avatar.invalid", FirebirdErrorCodes.AvatarInvalid.Value);
        Assert.Equal("firebird.username.not_found", FirebirdErrorCodes.UsernameNotFound.Value);
        Assert.Equal("firebird.interaction.validation", FirebirdErrorCodes.InteractionValidation.Value);
        Assert.Equal("firebird.catalog.unknown_reference", FirebirdErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("firebird.category.not_found", FirebirdErrorCodes.CategoryNotFound.Value);
        Assert.Equal("firebird.platform.not_found", FirebirdErrorCodes.PlatformNotFound.Value);
    }

    [Fact]
    public void Attachment_owner_uses_the_person_kind_and_only_accepts_images()
    {
        var owner = FirebirdAttachments.PersonOwner(42);

        Assert.Equal(("Firebird", "Person", "42"), (owner.Module, owner.EntityType, owner.EntityId));
        Assert.True(FirebirdAttachments.IsAvatarContentType("image/png"));
        Assert.False(FirebirdAttachments.IsAvatarContentType("application/pdf"));
    }

    [Fact]
    public void Launcher_attention_key_is_frozen()
    {
        Assert.Equal("firebird", FirebirdLauncherCard.ModuleKey);
    }

    [Fact]
    public void Person_request_serializes_birthday_month_day_to_the_frozen_wire_shape()
    {
        var request = new CreatePersonRequest(
            Name: "Ada Lovelace",
            CategoryId: 1,
            Status: "Unknown",
            BirthdayMonth: 12,
            BirthdayDay: 10,
            Notes: null,
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Ada Lovelace", root.GetProperty("name").GetString());
        Assert.Equal(1, root.GetProperty("categoryId").GetInt32());
        Assert.Equal("Unknown", root.GetProperty("status").GetString());
        Assert.Equal(12, root.GetProperty("birthdayMonth").GetInt32());
        Assert.Equal(10, root.GetProperty("birthdayDay").GetInt32());
        Assert.Equal("Public", root.GetProperty("visibility").GetString());
    }
}
