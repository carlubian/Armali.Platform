using System.Text.Json;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.UnitTests;

/// <summary>
/// Freezes the Wave 0 Configuration management contracts: catalog kinds and their
/// replacement rules, move directions, route templates, initialization keys, stable
/// error codes, and the request/response JSON wire shapes. Later waves implement
/// behavior against exactly these values.
/// </summary>
public sealed class ConfigurationContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Shared_catalog_kinds_are_frozen_in_canonical_order()
    {
        Assert.Equal(
            [ConfigurationCatalogKind.Suppliers, ConfigurationCatalogKind.CostCenters, ConfigurationCatalogKind.Currencies],
            ConfigurationCatalogKinds.All.Select(descriptor => descriptor.Kind).ToArray());
        Assert.Equal(
            ["suppliers", "cost-centers", "currencies"],
            ConfigurationCatalogKinds.RouteSegments);
    }

    [Fact]
    public void Catalog_replacement_rules_are_frozen()
    {
        var suppliers = ConfigurationCatalogKinds.ForKind(ConfigurationCatalogKind.Suppliers);
        Assert.False(suppliers.IsRequired);
        Assert.True(suppliers.SupportsClearing);
        Assert.False(suppliers.RequiresExchangeRateWhenReferenced);

        var costCenters = ConfigurationCatalogKinds.ForKind(ConfigurationCatalogKind.CostCenters);
        Assert.False(costCenters.IsRequired);
        Assert.True(costCenters.SupportsClearing);
        Assert.False(costCenters.RequiresExchangeRateWhenReferenced);

        var currencies = ConfigurationCatalogKinds.ForKind(ConfigurationCatalogKind.Currencies);
        Assert.True(currencies.IsRequired);
        Assert.False(currencies.SupportsClearing);
        Assert.True(currencies.RequiresExchangeRateWhenReferenced);
    }

    [Fact]
    public void Catalog_segments_resolve_to_their_kind()
    {
        Assert.True(ConfigurationCatalogKinds.TryResolveSegment("suppliers", out var suppliers));
        Assert.Equal(ConfigurationCatalogKind.Suppliers, suppliers.Kind);

        Assert.True(ConfigurationCatalogKinds.TryResolveSegment("cost-centers", out var costCenters));
        Assert.Equal(ConfigurationCatalogKind.CostCenters, costCenters.Kind);

        Assert.True(ConfigurationCatalogKinds.TryResolveSegment("currencies", out var currencies));
        Assert.Equal(ConfigurationCatalogKind.Currencies, currencies.Kind);
    }

    [Theory]
    [InlineData("categories")]
    [InlineData("Suppliers")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_catalog_segments_are_rejected(string? segment)
    {
        Assert.False(ConfigurationCatalogKinds.TryResolveSegment(segment, out _));
    }

    [Fact]
    public void Move_directions_parse_the_frozen_wire_vocabulary()
    {
        Assert.True(CatalogMoveDirections.TryParse("up", out var up));
        Assert.Equal(CatalogMoveDirection.Up, up);
        Assert.True(CatalogMoveDirections.TryParse("DOWN", out var down));
        Assert.Equal(CatalogMoveDirection.Down, down);

        Assert.Equal("up", CatalogMoveDirections.ToWireValue(CatalogMoveDirection.Up));
        Assert.Equal("down", CatalogMoveDirections.ToWireValue(CatalogMoveDirection.Down));
    }

    [Theory]
    [InlineData("left")]
    [InlineData("first")]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_move_directions_are_rejected(string? direction)
    {
        Assert.False(CatalogMoveDirections.TryParse(direction, out _));
    }

    [Fact]
    public void Initialization_keys_are_frozen()
    {
        Assert.Equal(
            ["configuration.suppliers", "configuration.cost-centers", "configuration.currencies", "capex.categories", "opex.categories", "inventory.categories", "inventory.locations", "travel.trip-types", "travel.expense-categories", "clothes.categories", "clothes.colors", "assets.categories", "assets.locations", "maintenance.types", "processes.categories"],
            ConfigurationInitializationKeys.All);
    }

    [Fact]
    public void Management_route_templates_are_frozen()
    {
        Assert.Equal("configuration/suppliers", ConfigurationApiRoutes.Suppliers);
        Assert.Equal("configuration/cost-centers", ConfigurationApiRoutes.CostCenters);
        Assert.Equal("configuration/currencies", ConfigurationApiRoutes.Currencies);
        Assert.Equal("/{id:int}", ConfigurationApiRoutes.ById);
        Assert.Equal("/{id:int}/move", ConfigurationApiRoutes.Move);
        Assert.Equal("/{id:int}/deletion-impact", ConfigurationApiRoutes.DeletionImpact);
        Assert.Equal("/{id:int}/replace-and-delete", ConfigurationApiRoutes.ReplaceAndDelete);
    }

    [Fact]
    public void Capex_category_route_templates_are_frozen()
    {
        Assert.Equal("capex/categories", CapexApiRoutes.Categories);
        Assert.Equal("/{categoryId:int}", CapexApiRoutes.CategoryById);
        Assert.Equal("/{categoryId:int}/move", CapexApiRoutes.CategoryMove);
        Assert.Equal("/{categoryId:int}/deletion-impact", CapexApiRoutes.CategoryDeletionImpact);
        Assert.Equal("/{categoryId:int}/replace-and-delete", CapexApiRoutes.CategoryReplaceAndDelete);
    }

    [Fact]
    public void Configuration_error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("configuration.catalog.not_found", ConfigurationErrorCodes.CatalogNotFound.Value);
        Assert.Equal("configuration.catalog.validation", ConfigurationErrorCodes.CatalogValidation.Value);
        Assert.Equal("configuration.catalog.duplicate_name", ConfigurationErrorCodes.CatalogDuplicateName.Value);
        Assert.Equal("configuration.currency.duplicate_code", ConfigurationErrorCodes.CurrencyDuplicateCode.Value);
        Assert.Equal("configuration.currency.invalid_code", ConfigurationErrorCodes.CurrencyInvalidCode.Value);
        Assert.Equal("configuration.catalog.required_not_empty", ConfigurationErrorCodes.CatalogRequiredNotEmpty.Value);
        Assert.Equal("configuration.catalog.referenced", ConfigurationErrorCodes.CatalogReferenced.Value);
        Assert.Equal("configuration.catalog.invalid_replacement", ConfigurationErrorCodes.CatalogInvalidReplacement.Value);
        Assert.Equal("configuration.catalog.exchange_rate_required", ConfigurationErrorCodes.CatalogExchangeRateRequired.Value);
        Assert.Equal("configuration.catalog.exchange_rate_invalid", ConfigurationErrorCodes.CatalogExchangeRateInvalid.Value);
        Assert.Equal("configuration.catalog.migration_conflict", ConfigurationErrorCodes.CatalogMigrationConflict.Value);
        Assert.Equal("configuration.catalog.migration_failed", ConfigurationErrorCodes.CatalogMigrationFailed.Value);
    }

    [Fact]
    public void Capex_category_error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("capex.category.not_found", CapexErrorCodes.CategoryNotFound.Value);
        Assert.Equal("capex.category.validation", CapexErrorCodes.CategoryValidation.Value);
        Assert.Equal("capex.category.duplicate_name", CapexErrorCodes.CategoryDuplicateName.Value);
        Assert.Equal("capex.category.required_not_empty", CapexErrorCodes.CategoryRequiredNotEmpty.Value);
        Assert.Equal("capex.category.referenced", CapexErrorCodes.CategoryReferenced.Value);
        Assert.Equal("capex.category.invalid_replacement", CapexErrorCodes.CategoryInvalidReplacement.Value);
        Assert.Equal("capex.category.migration_conflict", CapexErrorCodes.CategoryMigrationConflict.Value);
    }

    [Fact]
    public void Replacement_request_serializes_to_the_frozen_wire_shape()
    {
        var json = JsonSerializer.Serialize(
            new CatalogReplacementRequest(ReplacementId: 12, ClearReferences: false, ExchangeRate: null),
            Web);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(12, root.GetProperty("replacementId").GetInt32());
        Assert.False(root.GetProperty("clearReferences").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("exchangeRate").ValueKind);
    }

    [Fact]
    public void Deletion_impact_response_serializes_to_the_frozen_wire_shape()
    {
        var json = JsonSerializer.Serialize(
            new CatalogDeletionImpactResponse(
                IsReferenced: true,
                CanDeleteDirectly: false,
                CanClearReferences: true,
                RequiresExchangeRate: false,
                HasReplacementCandidates: true),
            Web);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.GetProperty("isReferenced").GetBoolean());
        Assert.False(root.GetProperty("canDeleteDirectly").GetBoolean());
        Assert.True(root.GetProperty("canClearReferences").GetBoolean());
        Assert.False(root.GetProperty("requiresExchangeRate").GetBoolean());
        Assert.True(root.GetProperty("hasReplacementCandidates").GetBoolean());
    }

    [Fact]
    public void Catalog_and_currency_create_requests_serialize_to_the_frozen_wire_shape()
    {
        using var item = JsonDocument.Parse(JsonSerializer.Serialize(new CatalogItemRequest("Household", "#123ABC"), Web));
        Assert.Equal("Household", item.RootElement.GetProperty("name").GetString());
        Assert.Equal("#123ABC", item.RootElement.GetProperty("colorValue").GetString());

        using var currency = JsonDocument.Parse(JsonSerializer.Serialize(new CurrencyItemRequest("Euro", "EUR"), Web));
        Assert.Equal("Euro", currency.RootElement.GetProperty("name").GetString());
        Assert.Equal("EUR", currency.RootElement.GetProperty("code").GetString());

        using var move = JsonDocument.Parse(JsonSerializer.Serialize(new CatalogMoveRequest("up"), Web));
        Assert.Equal("up", move.RootElement.GetProperty("direction").GetString());
    }
}
