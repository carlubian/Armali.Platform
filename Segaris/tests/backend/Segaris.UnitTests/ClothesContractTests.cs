using System.Text.Json;
using Segaris.Api.Modules.Clothes;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class ClothesContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Fixed_vocabularies_are_frozen()
    {
        Assert.Equal(["Active", "Unavailable", "Deprecated"], Enum.GetNames<ClothesGarmentStatus>());
        Assert.Equal(
            [
                "Any", "Wash30", "Wash30Delicate", "Wash40", "Wash40Delicate",
                "Wash50", "Wash50Delicate", "Wash60", "Wash60Delicate",
                "HandWash", "DoNotWash",
            ],
            Enum.GetNames<WashingCare>());
        Assert.Equal(["Any", "Delicate", "VeryDelicate"], Enum.GetNames<DryingCare>());
        Assert.Equal(["Any", "Low", "Medium", "DoNotIron"], Enum.GetNames<IroningCare>());
        Assert.Equal(["Any", "DoNotDryClean"], Enum.GetNames<DryCleaningCare>());
    }

    [Fact]
    public void Creation_defaults_are_frozen()
    {
        Assert.Equal(ClothesGarmentStatus.Active, ClothesDefaults.GarmentStatus);
        Assert.Equal(RecordVisibility.Public, ClothesDefaults.Visibility);
        Assert.Equal(200, ClothesDefaults.NameMaximumLength);
        Assert.Equal(50, ClothesDefaults.SizeMaximumLength);
        Assert.Equal(4000, ClothesDefaults.NotesMaximumLength);
    }

    [Fact]
    public void Routes_freeze_garments_catalogues_attachments_and_primary_image()
    {
        Assert.Equal("clothes/garments", ClothesApiRoutes.Garments);
        Assert.Equal("/{garmentId:int}", ClothesApiRoutes.GarmentById);
        Assert.Equal("/{garmentId:int}/attachments", ClothesApiRoutes.GarmentAttachments);
        Assert.Equal("/{garmentId:int}/attachments/{attachmentId}", ClothesApiRoutes.GarmentAttachmentById);
        Assert.Equal(
            "/{garmentId:int}/attachments/{attachmentId}/primary",
            ClothesApiRoutes.GarmentPrimaryAttachment);
        Assert.Equal("clothes/categories", ClothesApiRoutes.Categories);
        Assert.Equal("clothes/colors", ClothesApiRoutes.Colors);
    }

    [Fact]
    public void Garment_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "category", "status", "visibility", "id",
            },
            ClothesGarmentQuery.AllowedSortFields);
        Assert.Equal("name", ClothesGarmentQuery.SortFields.Default);
        Assert.Equal("id", ClothesGarmentQuery.SortFields.TieBreaker);
        Assert.Equal("asc", ClothesGarmentQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], ClothesGarmentQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_garment_sort_is_name_ascending()
    {
        var sort = SortRequest.Create(
            null,
            null,
            ClothesGarmentQuery.AllowedSortFields,
            ClothesGarmentQuery.SortFields.Default,
            ClothesGarmentQuery.SortFields.TieBreaker);

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
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(ClothesConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [ClothesCatalogKind.ClothingCategories, ClothesCatalogKind.ClothingColors],
            ClothesConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        var categories = ClothesConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("categories", categories.RouteSegment);
        Assert.True(categories.IsRequired);
        Assert.False(categories.SupportsClearing);
        Assert.False(categories.RequiresColorValue);

        var colors = ClothesConfigurationContracts.OwnedCatalogs[1];
        Assert.Equal("colors", colors.RouteSegment);
        Assert.False(colors.IsRequired);
        Assert.True(colors.SupportsClearing);
        Assert.True(colors.RequiresColorValue);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("clothes.garment.not_found", ClothesErrorCodes.GarmentNotFound.Value);
        Assert.Equal("clothes.garment.validation", ClothesErrorCodes.GarmentValidation.Value);
        Assert.Equal("clothes.garment.visibility_forbidden", ClothesErrorCodes.GarmentVisibilityForbidden.Value);
        Assert.Equal("clothes.catalog.unknown_reference", ClothesErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("clothes.attachment.not_found", ClothesErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("clothes.attachment.invalid", ClothesErrorCodes.AttachmentInvalid.Value);
        Assert.Equal("clothes.attachment.primary_invalid", ClothesErrorCodes.AttachmentPrimaryInvalid.Value);
        Assert.Equal("clothes.category.not_found", ClothesErrorCodes.CategoryNotFound.Value);
        Assert.Equal("clothes.color.not_found", ClothesErrorCodes.ColorNotFound.Value);
    }

    [Fact]
    public void Attachment_owner_uses_garment_kind()
    {
        var garment = ClothesAttachments.GarmentOwner(12);

        Assert.Equal(("Clothes", "Garment", "12"), (garment.Module, garment.EntityType, garment.EntityId));
    }

    [Fact]
    public void Garment_request_serializes_colours_and_care_axes_to_the_frozen_wire_shape()
    {
        var request = new CreateClothesGarmentRequest(
            "Jacket",
            CategoryId: 1,
            Status: "Active",
            Size: "M",
            ColorIds: [2, 3],
            WashingCare: "Wash30",
            DryingCare: "Delicate",
            IroningCare: "Low",
            DryCleaningCare: "DoNotDryClean",
            Notes: null,
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Jacket", root.GetProperty("name").GetString());
        Assert.Equal(1, root.GetProperty("categoryId").GetInt32());
        Assert.Equal([2, 3], root.GetProperty("colorIds").EnumerateArray().Select(value => value.GetInt32()).ToArray());
        Assert.Equal("Wash30", root.GetProperty("washingCare").GetString());
        Assert.Equal("DoNotDryClean", root.GetProperty("dryCleaningCare").GetString());
    }
}
