using System.Text.Json;
using Segaris.Api.Modules.Assets;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class AssetsContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Fixed_status_vocabulary_is_frozen()
    {
        Assert.Equal(["Active", "Stored", "Retired"], Enum.GetNames<AssetStatus>());
    }

    [Fact]
    public void Creation_defaults_are_frozen()
    {
        Assert.Equal(AssetStatus.Active, AssetDefaults.Status);
        Assert.Equal(RecordVisibility.Public, AssetDefaults.Visibility);
        Assert.Equal(200, AssetDefaults.NameMaximumLength);
        Assert.Equal(50, AssetDefaults.CodeMaximumLength);
        Assert.Equal(200, AssetDefaults.BrandModelMaximumLength);
        Assert.Equal(200, AssetDefaults.SerialNumberMaximumLength);
        Assert.Equal(4000, AssetDefaults.NotesMaximumLength);
    }

    [Fact]
    public void Routes_freeze_items_catalogues_attachments_and_primary_image()
    {
        Assert.Equal("assets/items", AssetsApiRoutes.Items);
        Assert.Equal("/{assetId:int}", AssetsApiRoutes.ItemById);
        Assert.Equal("/{assetId:int}/attachments", AssetsApiRoutes.ItemAttachments);
        Assert.Equal("/{assetId:int}/attachments/{attachmentId}", AssetsApiRoutes.ItemAttachmentById);
        Assert.Equal(
            "/{assetId:int}/attachments/{attachmentId}/primary",
            AssetsApiRoutes.ItemPrimaryAttachment);
        Assert.Equal("assets/categories", AssetsApiRoutes.Categories);
        Assert.Equal("assets/locations", AssetsApiRoutes.Locations);
    }

    [Fact]
    public void Asset_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "code", "category", "location", "status", "expectedEndOfLife", "visibility", "id",
            },
            AssetQuery.AllowedSortFields);
        Assert.Equal("name", AssetQuery.SortFields.Default);
        Assert.Equal("id", AssetQuery.SortFields.TieBreaker);
        Assert.Equal("asc", AssetQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], AssetQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_asset_sort_is_name_ascending()
    {
        var sort = SortRequest.Create(
            null,
            null,
            AssetQuery.AllowedSortFields,
            AssetQuery.SortFields.Default,
            AssetQuery.SortFields.TieBreaker);

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
        Assert.Empty(AssetsConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [AssetCatalogKind.AssetCategories, AssetCatalogKind.AssetLocations],
            AssetsConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        var categories = AssetsConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("categories", categories.RouteSegment);
        Assert.True(categories.IsRequired);
        Assert.False(categories.SupportsClearing);

        var locations = AssetsConfigurationContracts.OwnedCatalogs[1];
        Assert.Equal("locations", locations.RouteSegment);
        Assert.True(locations.IsRequired);
        Assert.False(locations.SupportsClearing);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("assets.asset.not_found", AssetsErrorCodes.AssetNotFound.Value);
        Assert.Equal("assets.asset.validation", AssetsErrorCodes.AssetValidation.Value);
        Assert.Equal("assets.asset.visibility_forbidden", AssetsErrorCodes.AssetVisibilityForbidden.Value);
        Assert.Equal("assets.asset.duplicate_code", AssetsErrorCodes.AssetDuplicateCode.Value);
        Assert.Equal("assets.catalog.unknown_reference", AssetsErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("assets.attachment.not_found", AssetsErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("assets.attachment.invalid", AssetsErrorCodes.AttachmentInvalid.Value);
        Assert.Equal("assets.attachment.primary_invalid", AssetsErrorCodes.AttachmentPrimaryInvalid.Value);
        Assert.Equal("assets.category.not_found", AssetsErrorCodes.CategoryNotFound.Value);
        Assert.Equal("assets.location.not_found", AssetsErrorCodes.LocationNotFound.Value);
    }

    [Fact]
    public void Attachment_owner_uses_asset_kind()
    {
        var asset = AssetsAttachments.AssetOwner(12);

        Assert.Equal(("Assets", "Asset", "12"), (asset.Module, asset.EntityType, asset.EntityId));
    }

    [Fact]
    public void Launcher_attention_key_is_frozen()
    {
        Assert.Equal("assets", AssetsLauncherCard.ModuleKey);
    }

    [Fact]
    public void Asset_request_serializes_codes_dates_and_references_to_the_frozen_wire_shape()
    {
        var request = new CreateAssetRequest(
            "Drill",
            CategoryId: 5,
            LocationId: 3,
            Status: "Active",
            Code: "TOOL-001",
            BrandModel: "Bosch GSB",
            SerialNumber: "SN-42",
            AcquisitionDate: new DateOnly(2026, 1, 15),
            ExpectedEndOfLifeDate: new DateOnly(2031, 1, 15),
            Notes: null,
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Drill", root.GetProperty("name").GetString());
        Assert.Equal(5, root.GetProperty("categoryId").GetInt32());
        Assert.Equal(3, root.GetProperty("locationId").GetInt32());
        Assert.Equal("TOOL-001", root.GetProperty("code").GetString());
        Assert.Equal("Bosch GSB", root.GetProperty("brandModel").GetString());
        Assert.Equal("SN-42", root.GetProperty("serialNumber").GetString());
        Assert.Equal("2026-01-15", root.GetProperty("acquisitionDate").GetString());
        Assert.Equal("2031-01-15", root.GetProperty("expectedEndOfLifeDate").GetString());
    }
}
