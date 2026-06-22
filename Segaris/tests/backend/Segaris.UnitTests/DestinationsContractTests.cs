using System.Text.Json;
using Segaris.Api.Modules.Destinations;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class DestinationsContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Creation_defaults_and_validation_bounds_are_frozen()
    {
        Assert.Equal(RecordVisibility.Public, DestinationsDefaults.Visibility);
        Assert.False(DestinationsDefaults.DefaultIsSchengenArea);
        Assert.Equal(200, DestinationsDefaults.NameMaximumLength);
        Assert.Equal(200, DestinationsDefaults.CountryMaximumLength);
        Assert.Equal(2000, DestinationsDefaults.EntryRequirementsMaximumLength);
        Assert.Equal(2000, DestinationsDefaults.NotesMaximumLength);
        Assert.Equal(200, DestinationsDefaults.PlaceNameMaximumLength);
        Assert.Equal(1, DestinationsDefaults.MinimumPlaceRating);
        Assert.Equal(5, DestinationsDefaults.MaximumPlaceRating);
        Assert.Equal(2000, DestinationsDefaults.PlaceReviewMaximumLength);
        Assert.Equal(200, DestinationsDefaults.PlaceAddressMaximumLength);
        Assert.Equal(200, DestinationsDefaults.CategoryNameMaximumLength);
        Assert.Equal(200, DestinationsDefaults.PlaceCategoryNameMaximumLength);
    }

    [Fact]
    public void Routes_freeze_destinations_places_categories_and_attachments()
    {
        Assert.Equal("destinations", DestinationsApiRoutes.Destinations);
        Assert.Equal("/{destinationId:int}", DestinationsApiRoutes.DestinationById);
        Assert.Equal("/{destinationId:int}/attachments", DestinationsApiRoutes.DestinationAttachments);
        Assert.Equal(
            "/{destinationId:int}/attachments/{attachmentId}",
            DestinationsApiRoutes.DestinationAttachmentById);
        Assert.Equal(
            "/{destinationId:int}/attachments/{attachmentId}/primary",
            DestinationsApiRoutes.DestinationPrimaryAttachment);
        Assert.Equal("/{destinationId:int}/places", DestinationsApiRoutes.Places);
        Assert.Equal("/{destinationId:int}/places/{placeId:int}", DestinationsApiRoutes.PlaceById);
        Assert.Equal("destinations/categories", DestinationsApiRoutes.Categories);
        Assert.Equal("destinations/place-categories", DestinationsApiRoutes.PlaceCategories);
    }

    [Fact]
    public void Destination_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "name", "category", "id" },
            DestinationQuery.AllowedSortFields);
        Assert.Equal("name", DestinationQuery.SortFields.Default);
        Assert.Equal("id", DestinationQuery.SortFields.TieBreaker);
        Assert.Equal("asc", DestinationQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], DestinationQuery.PageSizeOptions);
    }

    [Fact]
    public void Place_sort_pagination_and_rating_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "name", "category", "rating", "id" },
            PlaceQuery.AllowedSortFields);
        Assert.Equal("name", PlaceQuery.SortFields.Default);
        Assert.Equal("id", PlaceQuery.SortFields.TieBreaker);
        Assert.Equal("asc", PlaceQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], PlaceQuery.PageSizeOptions);
        Assert.Equal((1, 5), (DestinationsDefaults.MinimumPlaceRating, DestinationsDefaults.MaximumPlaceRating));
    }

    [Fact]
    public void Default_sorts_are_name_ascending_with_identifier_tie_breaker()
    {
        var destinationSort = SortRequest.Create(
            null,
            null,
            DestinationQuery.AllowedSortFields,
            DestinationQuery.SortFields.Default,
            DestinationQuery.SortFields.TieBreaker);
        var placeSort = SortRequest.Create(
            null,
            null,
            PlaceQuery.AllowedSortFields,
            PlaceQuery.SortFields.Default,
            PlaceQuery.SortFields.TieBreaker);

        Assert.Equal(("name", SortDirection.Ascending, "id"), (destinationSort.Field, destinationSort.Direction, destinationSort.TieBreakerField));
        Assert.Equal(("name", SortDirection.Ascending, "id"), (placeSort.Field, placeSort.Direction, placeSort.TieBreakerField));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Pagination_rejects_page_sizes_outside_platform_bounds(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(1, pageSize));
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(DestinationsConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [DestinationsCatalogKind.DestinationCategories, DestinationsCatalogKind.PlaceCategories],
            DestinationsConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        var destinationCategories = DestinationsConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("categories", destinationCategories.RouteSegment);
        Assert.True(destinationCategories.IsRequired);
        Assert.False(destinationCategories.SupportsClearing);

        var placeCategories = DestinationsConfigurationContracts.OwnedCatalogs[1];
        Assert.Equal("place-categories", placeCategories.RouteSegment);
        Assert.True(placeCategories.IsRequired);
        Assert.False(placeCategories.SupportsClearing);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("destinations.destination.not_found", DestinationsErrorCodes.DestinationNotFound.Value);
        Assert.Equal("destinations.destination.validation", DestinationsErrorCodes.DestinationValidation.Value);
        Assert.Equal("destinations.destination.visibility_forbidden", DestinationsErrorCodes.DestinationVisibilityForbidden.Value);
        Assert.Equal("destinations.place.not_found", DestinationsErrorCodes.PlaceNotFound.Value);
        Assert.Equal("destinations.place.validation", DestinationsErrorCodes.PlaceValidation.Value);
        Assert.Equal("destinations.catalog.unknown_reference", DestinationsErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("destinations.attachment.not_found", DestinationsErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("destinations.attachment.primary_invalid", DestinationsErrorCodes.AttachmentPrimaryInvalid.Value);
        Assert.Equal("destinations.category.not_found", DestinationsErrorCodes.CategoryNotFound.Value);
        Assert.Equal("destinations.category.referenced", DestinationsErrorCodes.CategoryReferenced.Value);
        Assert.Equal("destinations.place_category.not_found", DestinationsErrorCodes.PlaceCategoryNotFound.Value);
        Assert.Equal("destinations.place_category.referenced", DestinationsErrorCodes.PlaceCategoryReferenced.Value);
    }

    [Fact]
    public void Attachment_owner_uses_destination_kind()
    {
        var destination = DestinationsAttachments.DestinationOwner(12);

        Assert.Equal(("Destinations", "Destination", "12"), (destination.Module, destination.EntityType, destination.EntityId));
    }

    [Fact]
    public void Destination_request_serializes_to_the_frozen_wire_shape()
    {
        var request = new CreateDestinationRequest(
            "Barcelona",
            CategoryId: 1,
            Country: "Spain",
            EntryRequirements: null,
            IsSchengenArea: true,
            Notes: "Metro cards",
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Barcelona", root.GetProperty("name").GetString());
        Assert.Equal(1, root.GetProperty("categoryId").GetInt32());
        Assert.Equal("Spain", root.GetProperty("country").GetString());
        Assert.True(root.GetProperty("isSchengenArea").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("entryRequirements").ValueKind);
    }

    [Fact]
    public void Place_request_serializes_rating_review_and_address_to_the_frozen_wire_shape()
    {
        var request = new CreatePlaceRequest(
            "Hotel",
            CategoryId: 2,
            Rating: 5,
            Review: "Excellent",
            Address: "Main street");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Hotel", root.GetProperty("name").GetString());
        Assert.Equal(2, root.GetProperty("categoryId").GetInt32());
        Assert.Equal(5, root.GetProperty("rating").GetInt32());
        Assert.Equal("Excellent", root.GetProperty("review").GetString());
        Assert.Equal("Main street", root.GetProperty("address").GetString());
    }

    [Fact]
    public void Update_place_request_shares_the_frozen_place_wire_shape()
    {
        var request = new UpdatePlaceRequest(
            "Museum",
            CategoryId: 3,
            Rating: null,
            Review: null,
            Address: null);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Museum", root.GetProperty("name").GetString());
        Assert.Equal(3, root.GetProperty("categoryId").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("rating").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("review").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("address").ValueKind);
    }
}
