using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.IntegrationTests.Inventory;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Recipes;

public sealed class RecipesWave3IngredientItemTests
{
    [Fact]
    public async Task Create_update_and_detail_resolve_and_clear_ingredient_item_links()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Flour");
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Dessert");

        using var createdResponse = await CapexApi.PostJsonAsync(
            client,
            "/api/recipes",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithIngredients(new RecipeIngredientRequest("All-purpose flour", "500g", itemId))
                .BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<RecipeResponse>(CancellationToken.None);

        using var updatedResponse = await CapexApi.PutJsonAsync(
            client,
            $"/api/recipes/{created!.Id}",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithIngredients(new RecipeIngredientRequest("All-purpose flour", "500g", ItemId: null))
                .BuildUpdate(),
            csrf);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<RecipeResponse>(CancellationToken.None);

        var linked = Assert.Single(created.Ingredients);
        Assert.Equal(itemId, linked.ItemId);
        Assert.Equal("Flour", linked.ItemName);
        var unlinked = Assert.Single(updated!.Ingredients);
        Assert.Null(unlinked.ItemId);
        Assert.Null(unlinked.ItemName);
    }

    [Fact]
    public async Task Ingredient_item_references_enforce_accessibility_and_visibility()
    {
        using var server = new CapexTestServer();
        using var ownerClient = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(ownerClient);
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            ownerId,
            name: "Private saffron",
            visibility: RecordVisibility.Private);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Other");

        using var publicRecipe = await CapexApi.PostJsonAsync(
            ownerClient,
            "/api/recipes",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithVisibility("Public")
                .WithIngredients(new RecipeIngredientRequest("Saffron", "1 pinch", privateItemId))
                .BuildCreate(),
            ownerCsrf);
        var publicProblem = await publicRecipe.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        await server.CreateUserAsync("recipes-wave3-member", "RecipesWave3Member123!");
        using var memberClient = server.CreateClient();
        await CapexTestServer.LoginAsync(memberClient, "recipes-wave3-member", "RecipesWave3Member123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(memberClient);
        using var inaccessibleItem = await CapexApi.PostJsonAsync(
            memberClient,
            "/api/recipes",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithVisibility("Private")
                .WithIngredients(new RecipeIngredientRequest("Saffron", "1 pinch", privateItemId))
                .BuildCreate(),
            memberCsrf);
        var inaccessibleProblem = await inaccessibleItem.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, publicRecipe.StatusCode);
        Assert.Equal("recipes.ingredient.item_visibility_forbidden", publicProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, inaccessibleItem.StatusCode);
        Assert.Equal("recipes.ingredient.item_not_accessible", inaccessibleProblem!.Code);
    }

    [Fact]
    public async Task Visibility_and_item_changes_cannot_make_a_public_recipe_reference_a_private_item()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Private butter",
            visibility: RecordVisibility.Private);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Other");
        var privateRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Private cake",
            ingredients: [new("Butter", "200g", privateItemId)],
            visibility: RecordVisibility.Private);
        var publicRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Public cake",
            visibility: RecordVisibility.Public);

        using var makePublic = await CapexApi.PutJsonAsync(
            client,
            $"/api/recipes/{privateRecipeId}",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithVisibility("Public")
                .WithIngredients(new RecipeIngredientRequest("Butter", "200g", privateItemId))
                .BuildUpdate(),
            csrf);
        using var addPrivateItem = await CapexApi.PutJsonAsync(
            client,
            $"/api/recipes/{publicRecipeId}",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithVisibility("Public")
                .WithIngredients(new RecipeIngredientRequest("Butter", "200g", privateItemId))
                .BuildUpdate(),
            csrf);

        var makePublicProblem = await makePublic.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var addPrivateItemProblem = await addPrivateItem.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Forbidden, makePublic.StatusCode);
        Assert.Equal("recipes.ingredient.item_visibility_forbidden", makePublicProblem!.Code);
        Assert.Equal(HttpStatusCode.Forbidden, addPrivateItem.StatusCode);
        Assert.Equal("recipes.ingredient.item_visibility_forbidden", addPrivateItemProblem!.Code);
    }

    [Fact]
    public async Task Public_recipe_detail_does_not_resolve_private_item_names_for_other_users()
    {
        using var server = new CapexTestServer();
        using var ownerClient = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(ownerClient);
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, ownerId, name: "Vanilla");
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Dessert");

        using var createdResponse = await CapexApi.PostJsonAsync(
            ownerClient,
            "/api/recipes",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithVisibility("Public")
                .WithIngredients(new RecipeIngredientRequest("Vanilla extract", "1 tsp", itemId))
                .BuildCreate(),
            ownerCsrf);
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<RecipeResponse>(CancellationToken.None);

        var privateItemUpdate = (await InventoryItemMutationTests.DefaultBuilderAsync(server))
            .WithName("Vanilla")
            .WithStatus("Candidate")
            .WithVisibility("Private")
            .BuildUpdate();
        using var itemUpdated = await CapexApi.PutJsonAsync(ownerClient, $"/api/inventory/items/{itemId}", privateItemUpdate, ownerCsrf);
        itemUpdated.EnsureSuccessStatusCode();

        await server.CreateUserAsync("recipes-wave3-viewer", "RecipesWave3Viewer123!");
        using var viewerClient = server.CreateClient();
        await CapexTestServer.LoginAsync(viewerClient, "recipes-wave3-viewer", "RecipesWave3Viewer123!");

        var detail = await viewerClient.GetFromJsonAsync<RecipeResponse>($"/api/recipes/{created!.Id}", CancellationToken.None);

        var ingredient = Assert.Single(detail!.Ingredients);
        Assert.Equal(itemId, ingredient.ItemId);
        Assert.Null(ingredient.ItemName);
        Assert.Equal("Vanilla extract", ingredient.Name);
    }

    private sealed record ProblemPayload(string? Code);
}
