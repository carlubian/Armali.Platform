using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Recipes;

public sealed class RecipesWave2EndpointTests
{
    [Fact]
    public async Task Recipes_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/recipes", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_ingredients_and_steps()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Breakfast");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/recipes",
            RecipeRequestBuilder.Default()
                .WithName("  Spanish tortilla  ")
                .WithCategory(categoryId)
                .WithDifficulty("Easy")
                .WithServings(4)
                .WithPreparationMinutes(10)
                .WithCookMinutes(20)
                .WithIngredients(
                    new RecipeIngredientRequest(" Eggs ", " 4 units ", ItemId: null),
                    new RecipeIngredientRequest("Salt", "", ItemId: null))
                .WithSteps(
                    new RecipeStepRequest(" Beat eggs "),
                    new RecipeStepRequest("Cook slowly"))
                .WithVisibility(null)
                .BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RecipeResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Spanish tortilla", created.Name);
        Assert.Equal("Easy", created.Difficulty);
        Assert.Equal(4, created.Servings);
        Assert.Equal(10, created.PreparationMinutes);
        Assert.Equal(20, created.CookMinutes);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal("placeholder", created.Thumbnail.Source);
        Assert.Empty(created.Attachments);
        Assert.Equal(["Eggs", "Salt"], created.Ingredients.Select(ingredient => ingredient.Name).ToArray());
        Assert.Equal("4 units", created.Ingredients[0].Quantity);
        Assert.Null(created.Ingredients[1].Quantity);
        Assert.All(created.Ingredients, ingredient => Assert.Null(ingredient.ItemId));
        Assert.Equal([0, 1], created.Ingredients.Select(ingredient => ingredient.Position).ToArray());
        Assert.Equal(["Beat eggs", "Cook slowly"], created.Steps.Select(step => step.Instruction).ToArray());
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_and_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Apple pie",
            categoryName: "Dessert",
            difficulty: RecipeDifficulty.Medium,
            notes: "family cinnamon favourite");
        await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Avocado toast",
            categoryName: "Breakfast",
            difficulty: RecipeDifficulty.Easy);
        await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Beef stew",
            categoryName: "Main",
            difficulty: RecipeDifficulty.Hard,
            visibility: RecordVisibility.Private);

        var dessertId = await RecipesTestData.CategoryIdAsync(server.Services, "Dessert");

        var firstPage = await GetPageAsync(client, "/api/recipes?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/recipes?search=CINNAMON");
        var byCategory = await GetPageAsync(client, $"/api/recipes?category={dessertId}");
        var byDifficulty = await GetPageAsync(client, "/api/recipes?difficulty=Hard");
        var byCategorySort = await GetPageAsync(client, "/api/recipes?sort=category&sortDirection=asc");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal("Apple pie", Assert.Single(search.Items).Name);
        Assert.Equal("Apple pie", Assert.Single(byCategory.Items).Name);
        Assert.Equal("Beef stew", Assert.Single(byDifficulty.Items).Name);
        Assert.Equal(["Breakfast", "Dessert", "Main"], byCategorySort.Items.Select(item => item.CategoryName).ToArray());
    }

    [Fact]
    public async Task Detail_update_and_delete_manage_the_complete_recipe()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var recipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Original",
            ingredients: [new("Egg", null, null), new("Salt", null, null)],
            steps: [new("Mix"), new("Cook")]);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Main");

        var detail = await client.GetFromJsonAsync<RecipeResponse>($"/api/recipes/{recipeId}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/recipes/{recipeId}",
            RecipeRequestBuilder.Default()
                .WithName("Updated stew")
                .WithCategory(categoryId)
                .WithDifficulty("Hard")
                .WithServings(6)
                .WithPreparationMinutes(15)
                .WithCookMinutes(90)
                .WithIngredients(new RecipeIngredientRequest("Beef", "500g", ItemId: null))
                .WithSteps(new RecipeStepRequest("Simmer"))
                .WithNotes("Serve warm")
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<RecipeResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/recipes/{recipeId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Name);
        Assert.Equal(["Egg", "Salt"], detail.Ingredients.Select(ingredient => ingredient.Name).ToArray());
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated stew", updated!.Name);
        Assert.Equal("Hard", updated.Difficulty);
        Assert.Equal("Private", updated.Visibility);
        Assert.Equal("Beef", Assert.Single(updated.Ingredients).Name);
        Assert.Equal("Simmer", Assert.Single(updated.Steps).Instruction);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await RecipesTestData.RecipeExistsAsync(server.Services, recipeId));
    }

    [Fact]
    public async Task Unknown_references_and_invalid_values_return_recipe_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Other");

        using var unknown = await CapexApi.PostJsonAsync(
            client,
            "/api/recipes",
            RecipeRequestBuilder.Default().WithCategory(999_999).BuildCreate(),
            csrf);
        using var invalid = await CapexApi.PostJsonAsync(
            client,
            "/api/recipes",
            RecipeRequestBuilder.Default().WithCategory(categoryId).WithDifficulty("Extreme").BuildCreate(),
            csrf);

        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("recipes.catalog.unknown_reference", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("recipes.recipe.validation", invalidProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_and_private_isolation_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Shared",
            visibility: RecordVisibility.Public);
        var privateRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Other");

        await server.CreateUserAsync("recipes-member", "RecipesMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "recipes-member", "RecipesMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/recipes/{publicRecipeId}",
            RecipeRequestBuilder.Default().WithName("Shared edited").WithCategory(categoryId).BuildUpdate(),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/recipes/{privateRecipeId}",
            RecipeRequestBuilder.Default().WithName("Private edited").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/recipes/{publicRecipeId}",
            RecipeRequestBuilder.Default().WithName("Shared hidden").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var getPrivate = await member.GetAsync($"/api/recipes/{privateRecipeId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getPrivate.StatusCode);
    }

    private static async Task<PaginatedResponse<RecipeSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<RecipeSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}
