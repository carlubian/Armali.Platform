using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.IntegrationTests.Recipes;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemDeletionReferenceTests
{
    [Fact]
    public async Task Deletion_impact_reports_recipe_references_without_disclosing_records()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("inventory-wave4-member", "InventoryWave4Member123!");
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Flour");
        await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Public bread",
            ingredients: [new RecipeIngredientValues("Flour", "500g", itemId)],
            visibility: RecordVisibility.Public);
        await RecipesTestData.SeedRecipeAsync(
            server.Services,
            memberId,
            name: "Private pancakes",
            ingredients: [new RecipeIngredientValues("Flour", "200g", itemId)],
            visibility: RecordVisibility.Private);

        using var response = await client.GetAsync($"/api/inventory/items/{itemId}/deletion-impact", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var impact = await response.Content.ReadFromJsonAsync<InventoryItemDeletionImpactResponse>(CancellationToken.None);
        Assert.NotNull(impact);
        Assert.True(impact.IsReferenced);
        Assert.Equal(2, impact.ReferenceCount);
    }

    [Fact]
    public async Task Delete_clears_recipe_ingredient_item_links_across_mixed_ownership_recipes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("inventory-wave4-owner", "InventoryWave4Owner123!");
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Tomato");
        var publicRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Gazpacho",
            ingredients: [new RecipeIngredientValues("Tomato", "4 units", itemId)],
            visibility: RecordVisibility.Public);
        var privateRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            memberId,
            name: "Private sauce",
            ingredients: [new RecipeIngredientValues("Tomato", "2 units", itemId)],
            visibility: RecordVisibility.Private);

        using var response = await CapexApi.DeleteAsync(client, $"/api/inventory/items/{itemId}", csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await InventoryTestData.ItemExistsAsync(server.Services, itemId));
        Assert.Equal([null], await RecipesTestData.IngredientItemIdsAsync(server.Services, publicRecipeId));
        Assert.Equal([null], await RecipesTestData.IngredientItemIdsAsync(server.Services, privateRecipeId));
    }

    [Fact]
    public async Task Delete_rolls_back_item_and_recipe_links_when_a_reference_handler_fails()
    {
        using var server = new CapexTestServer(configureServices: services =>
            services.AddScoped<IInventoryItemDeletionReferenceHandler, FailingInventoryItemDeletionReferenceHandler>());
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Rice");
        var recipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Rice bowl",
            ingredients: [new RecipeIngredientValues("Rice", "250g", itemId)],
            visibility: RecordVisibility.Public);

        using var response = await CapexApi.DeleteAsync(client, $"/api/inventory/items/{itemId}", csrf);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(await InventoryTestData.ItemExistsAsync(server.Services, itemId));
        Assert.Equal([itemId], await RecipesTestData.IngredientItemIdsAsync(server.Services, recipeId));
    }

    private sealed class FailingInventoryItemDeletionReferenceHandler : IInventoryItemDeletionReferenceHandler
    {
        public Task<int> CountReferencesAsync(int itemId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task ClearReferencesAsync(
            InventoryItemDeletionClearing clearing,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected test failure after earlier handlers run.");
    }
}
