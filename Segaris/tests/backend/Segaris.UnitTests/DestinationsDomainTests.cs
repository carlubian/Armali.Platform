using Segaris.Api.Modules.Destinations;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.UnitTests;

public sealed class DestinationsDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly UserId Creator = new(1);
    private static readonly UserId Other = new(2);

    // ── Destination validation and defaults ─────────────────────────────────────

    [Fact]
    public void Destination_trims_name_country_and_stamps_audit()
    {
        var destination = Destination.Create(
            DestinationValues(name: " Barcelona ", country: " Spain "),
            Creator,
            Now);

        Assert.Equal("Barcelona", destination.Name);
        Assert.Equal("Spain", destination.Country);
        Assert.Equal(Creator.Value, destination.CreatedBy);
        Assert.Equal(Creator.Value, destination.UpdatedBy);
        Assert.Equal(Now, destination.CreatedAt);
        Assert.Equal(Now, destination.UpdatedAt);
    }

    [Fact]
    public void Destination_defaults_schengen_flag_to_false()
    {
        var destination = Destination.Create(DestinationValues(), Creator, Now);
        Assert.False(destination.IsSchengenArea);
    }

    [Fact]
    public void Destination_preserves_schengen_flag_when_set()
    {
        var destination = Destination.Create(DestinationValues(isSchengenArea: true), Creator, Now);
        Assert.True(destination.IsSchengenArea);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Destination_rejects_blank_name(string name)
    {
        Assert.Throws<DestinationsValidationException>(
            () => Destination.Create(DestinationValues(name: name), Creator, Now));
    }

    [Fact]
    public void Destination_rejects_invalid_category()
    {
        Assert.Throws<DestinationsValidationException>(
            () => Destination.Create(DestinationValues(categoryId: 0), Creator, Now));
    }

    [Fact]
    public void Destination_rejects_invalid_visibility()
    {
        Assert.Throws<DestinationsValidationException>(
            () => Destination.Create(DestinationValues(visibility: (RecordVisibility)99), Creator, Now));
    }

    [Fact]
    public void Destination_rejects_non_utc_timestamp()
    {
        var localNow = new DateTimeOffset(2026, 6, 22, 10, 0, 0, TimeSpan.FromHours(2));
        Assert.Throws<DestinationsValidationException>(
            () => Destination.Create(DestinationValues(), Creator, localNow));
    }

    [Fact]
    public void Destination_normalizes_blank_optional_fields_to_null()
    {
        var destination = Destination.Create(
            DestinationValues(country: "   ", entryRequirements: "", notes: ""),
            Creator,
            Now);

        Assert.Null(destination.Country);
        Assert.Null(destination.EntryRequirements);
        Assert.Null(destination.Notes);
    }

    [Fact]
    public void Destination_allows_creator_to_change_visibility()
    {
        var destination = Destination.Create(DestinationValues(visibility: RecordVisibility.Public), Creator, Now);
        destination.Update(DestinationValues(visibility: RecordVisibility.Private), Creator, Now.AddHours(1));
        Assert.Equal(RecordVisibility.Private, destination.Visibility);
    }

    [Fact]
    public void Destination_forbids_non_creator_visibility_change()
    {
        var destination = Destination.Create(DestinationValues(visibility: RecordVisibility.Public), Creator, Now);
        var exception = Assert.Throws<DestinationsValidationException>(
            () => destination.Update(DestinationValues(visibility: RecordVisibility.Private), Other, Now.AddHours(1)));
        Assert.Equal(DestinationsValidationReason.VisibilityForbidden, exception.Reason);
    }

    [Fact]
    public void Destination_allows_non_creator_edit_without_visibility_change()
    {
        var destination = Destination.Create(DestinationValues(name: "Barcelona", visibility: RecordVisibility.Public), Creator, Now);
        destination.Update(DestinationValues(name: "Madrid", visibility: RecordVisibility.Public), Other, Now.AddHours(1));
        Assert.Equal("Madrid", destination.Name);
        Assert.Equal(Other.Value, destination.UpdatedBy);
    }

    [Fact]
    public void Destination_replace_category_stamps_modification()
    {
        var destination = Destination.Create(DestinationValues(categoryId: 1), Creator, Now);
        var later = Now.AddHours(1);
        destination.ReplaceCategory(2, Creator, later);

        Assert.Equal(2, destination.CategoryId);
        Assert.Equal(later, destination.UpdatedAt);
    }

    [Fact]
    public void Destination_set_and_clear_primary_attachment()
    {
        var destination = Destination.Create(DestinationValues(), Creator, Now);
        destination.SetPrimaryAttachment(5, Creator, Now.AddHours(1));
        Assert.Equal(5, destination.PrimaryAttachmentId);

        destination.ClearPrimaryAttachmentIf(5, Creator, Now.AddHours(2));
        Assert.Null(destination.PrimaryAttachmentId);
    }

    [Fact]
    public void Destination_clear_primary_attachment_ignores_other_ids()
    {
        var destination = Destination.Create(DestinationValues(), Creator, Now);
        destination.SetPrimaryAttachment(5, Creator, Now.AddHours(1));
        destination.ClearPrimaryAttachmentIf(9, Creator, Now.AddHours(2));
        Assert.Equal(5, destination.PrimaryAttachmentId);
    }

    // ── Place validation, ownership, and rating bounds ──────────────────────────

    [Fact]
    public void Place_belongs_to_its_owning_destination()
    {
        var place = Place.Create(42, PlaceValues(), Creator, Now);
        Assert.Equal(42, place.DestinationId);
    }

    [Fact]
    public void Place_rejects_non_positive_destination_id()
    {
        Assert.Throws<DestinationsValidationException>(
            () => Place.Create(0, PlaceValues(), Creator, Now));
    }

    [Fact]
    public void Place_trims_name_and_address()
    {
        var place = Place.Create(1, PlaceValues(name: " Hotel ", address: " Main street "), Creator, Now);
        Assert.Equal("Hotel", place.Name);
        Assert.Equal("Main street", place.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Place_rejects_blank_name(string name)
    {
        Assert.Throws<DestinationsValidationException>(
            () => Place.Create(1, PlaceValues(name: name), Creator, Now));
    }

    [Fact]
    public void Place_rejects_invalid_category()
    {
        Assert.Throws<DestinationsValidationException>(
            () => Place.Create(1, PlaceValues(categoryId: 0), Creator, Now));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Place_accepts_rating_within_bounds(int rating)
    {
        var place = Place.Create(1, PlaceValues(rating: rating), Creator, Now);
        Assert.Equal(rating, place.Rating);
    }

    [Fact]
    public void Place_accepts_absent_rating()
    {
        var place = Place.Create(1, PlaceValues(rating: null), Creator, Now);
        Assert.Null(place.Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Place_rejects_rating_out_of_bounds(int rating)
    {
        Assert.Throws<DestinationsValidationException>(
            () => Place.Create(1, PlaceValues(rating: rating), Creator, Now));
    }

    [Fact]
    public void Place_normalizes_blank_optional_fields_to_null()
    {
        var place = Place.Create(1, PlaceValues(review: "", address: "   "), Creator, Now);
        Assert.Null(place.Review);
        Assert.Null(place.Address);
    }

    [Fact]
    public void Place_replace_category_stamps_modification()
    {
        var place = Place.Create(1, PlaceValues(categoryId: 1), Creator, Now);
        var later = Now.AddHours(1);
        place.ReplaceCategory(2, Creator, later);

        Assert.Equal(2, place.CategoryId);
        Assert.Equal(later, place.UpdatedAt);
    }

    // ── Catalogue normalization and seeds ───────────────────────────────────────

    [Theory]
    [InlineData(" City ", "CITY")]
    [InlineData("natural area", "NATURAL AREA")]
    [InlineData("Café", "CAFÉ")]
    public void Catalogue_normalization_trims_and_folds_case(string input, string expected)
    {
        Assert.Equal(expected, DestinationsCatalogNormalization.Normalize(input));
    }

    [Fact]
    public void Initial_catalogue_seeds_match_the_accepted_values()
    {
        Assert.Equal(
            ["City", "Region", "Country", "Natural Area", "Other"],
            DestinationsCatalog.DestinationCategories.Select(seed => seed.Name).ToArray());
        Assert.Equal(
            ["Hotel", "Restaurant", "Bar", "Café", "Museum", "Attraction", "Shop", "Other"],
            DestinationsCatalog.PlaceCategories.Select(seed => seed.Name).ToArray());
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static DestinationValues DestinationValues(
        string? name = "Barcelona",
        int categoryId = 1,
        string? country = null,
        string? entryRequirements = null,
        bool isSchengenArea = false,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public) =>
        new(name, categoryId, country, entryRequirements, isSchengenArea, notes, visibility);

    private static PlaceValues PlaceValues(
        string? name = "Hotel",
        int categoryId = 1,
        int? rating = null,
        string? review = null,
        string? address = null) =>
        new(name, categoryId, rating, review, address);
}
