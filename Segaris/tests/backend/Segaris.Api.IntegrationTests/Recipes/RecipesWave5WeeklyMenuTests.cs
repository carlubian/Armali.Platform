using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Recipes;

public sealed class RecipesWave5WeeklyMenuTests
{
    [Fact]
    public async Task Create_list_detail_update_and_delete_manage_weekly_menus()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tortillaId = await RecipesTestData.SeedRecipeAsync(server.Services, founderId, name: "Tortilla");
        var soupId = await RecipesTestData.SeedRecipeAsync(server.Services, founderId, name: "Soup");

        using var createdResponse = await CapexApi.PostJsonAsync(
            client,
            "/api/recipes/menus",
            WeeklyMenuRequestBuilder.Default()
                .WithWeek(new DateOnly(2026, 6, 25))
                .WithName("  Week menu  ")
                .WithSlots(new WeeklyMenuSlotRequest("Monday", "Lunch", [tortillaId, soupId]))
                .BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<WeeklyMenuResponse>(CancellationToken.None);

        await RecipesTestData.SeedWeeklyMenuAsync(
            server.Services,
            founderId,
            week: new DateOnly(2026, 6, 26),
            name: "Second menu same week");
        var weekMenus = await client.GetFromJsonAsync<IReadOnlyList<WeeklyMenuSummaryResponse>>(
            "/api/recipes/menus?week=2026-06-28",
            CancellationToken.None);

        using var updatedResponse = await CapexApi.PutJsonAsync(
            client,
            $"/api/recipes/menus/{created!.Id}",
            WeeklyMenuRequestBuilder.Default()
                .WithWeek(new DateOnly(2026, 6, 29))
                .WithName(null)
                .WithVisibility("Private")
                .WithSlots(new WeeklyMenuSlotRequest("Tuesday", "Dinner", [soupId]))
                .BuildUpdate(),
            csrf);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<WeeklyMenuResponse>(CancellationToken.None);

        using var deletedResponse = await CapexApi.DeleteAsync(client, $"/api/recipes/menus/{created.Id}", csrf);

        Assert.NotNull(created);
        Assert.Equal(new DateOnly(2026, 6, 22), created.Week);
        Assert.Equal("Week menu", created.Name);
        Assert.Equal(28, created.Slots.Count);
        var lunch = Slot(created, "Monday", "Lunch");
        Assert.Equal([tortillaId, soupId], lunch.Recipes.Select(recipe => recipe.RecipeId).ToArray());
        Assert.Equal(["Soup", "Tortilla"], lunch.Recipes.Select(recipe => recipe.RecipeName!).Order().ToArray());
        Assert.All(lunch.Recipes, recipe => Assert.Equal("placeholder", recipe.Thumbnail.Source));
        Assert.NotNull(weekMenus);
        Assert.Equal(2, weekMenus.Count);
        Assert.All(weekMenus, menu => Assert.Equal(new DateOnly(2026, 6, 22), menu.Week));
        Assert.Equal(new DateOnly(2026, 6, 29), updated!.Week);
        Assert.Null(updated.Name);
        Assert.Equal("Private", updated.Visibility);
        Assert.Equal([soupId], Slot(updated, "Tuesday", "Dinner").Recipes.Select(recipe => recipe.RecipeId).ToArray());
        Assert.Equal(HttpStatusCode.NoContent, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task Menu_recipe_references_enforce_accessibility_and_visibility()
    {
        using var server = new CapexTestServer();
        using var ownerClient = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(ownerClient);
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicRecipeId = await RecipesTestData.SeedRecipeAsync(server.Services, ownerId, name: "Shared");
        var privateRecipeId = await RecipesTestData.SeedRecipeAsync(
            server.Services,
            ownerId,
            name: "Private",
            visibility: RecordVisibility.Private);

        using var publicWithPrivate = await CapexApi.PostJsonAsync(
            ownerClient,
            "/api/recipes/menus",
            WeeklyMenuRequestBuilder.Default()
                .WithVisibility("Public")
                .WithSlots(new WeeklyMenuSlotRequest("Monday", "Lunch", [privateRecipeId]))
                .BuildCreate(),
            ownerCsrf);
        var publicProblem = await publicWithPrivate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        using var privateMenuResponse = await CapexApi.PostJsonAsync(
            ownerClient,
            "/api/recipes/menus",
            WeeklyMenuRequestBuilder.Default()
                .WithVisibility("Private")
                .WithSlots(new WeeklyMenuSlotRequest("Monday", "Lunch", [privateRecipeId]))
                .BuildCreate(),
            ownerCsrf);
        Assert.Equal(HttpStatusCode.Created, privateMenuResponse.StatusCode);

        await server.CreateUserAsync("recipes-wave5-member", "RecipesWave5Member123!");
        using var memberClient = server.CreateClient();
        await CapexTestServer.LoginAsync(memberClient, "recipes-wave5-member", "RecipesWave5Member123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(memberClient);
        using var inaccessibleRecipe = await CapexApi.PostJsonAsync(
            memberClient,
            "/api/recipes/menus",
            WeeklyMenuRequestBuilder.Default()
                .WithVisibility("Private")
                .WithSlots(new WeeklyMenuSlotRequest("Monday", "Lunch", [privateRecipeId]))
                .BuildCreate(),
            memberCsrf);
        var inaccessibleProblem = await inaccessibleRecipe.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        using var memberPublicMenu = await CapexApi.PostJsonAsync(
            memberClient,
            "/api/recipes/menus",
            WeeklyMenuRequestBuilder.Default()
                .WithVisibility("Public")
                .WithSlots(new WeeklyMenuSlotRequest("Monday", "Lunch", [publicRecipeId]))
                .BuildCreate(),
            memberCsrf);

        Assert.Equal(HttpStatusCode.Forbidden, publicWithPrivate.StatusCode);
        Assert.Equal("recipes.menu.recipe_visibility_forbidden", publicProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, inaccessibleRecipe.StatusCode);
        Assert.Equal("recipes.menu.recipe_not_accessible", inaccessibleProblem!.Code);
        Assert.Equal(HttpStatusCode.Created, memberPublicMenu.StatusCode);
    }

    [Fact]
    public async Task Recipe_visibility_change_cannot_expose_private_recipe_through_public_menu()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var recipeId = await RecipesTestData.SeedRecipeAsync(server.Services, founderId, name: "Menu recipe");
        await RecipesTestData.SeedWeeklyMenuAsync(
            server.Services,
            founderId,
            slots: [new WeeklyMenuSlotValues("Monday", "Lunch", [recipeId])]);
        var categoryId = await RecipesTestData.CategoryIdAsync(server.Services, "Other");

        using var makePrivate = await CapexApi.PutJsonAsync(
            client,
            $"/api/recipes/{recipeId}",
            RecipeRequestBuilder.Default()
                .WithCategory(categoryId)
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var problem = await makePrivate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
        Assert.Equal("recipes.menu.recipe_visibility_forbidden", problem!.Code);
    }

    [Fact]
    public async Task Recipe_deletion_removes_slot_references_without_blocking_deletion()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var deletedRecipeId = await RecipesTestData.SeedRecipeAsync(server.Services, founderId, name: "Deleted");
        var remainingRecipeId = await RecipesTestData.SeedRecipeAsync(server.Services, founderId, name: "Remaining");
        var menuId = await RecipesTestData.SeedWeeklyMenuAsync(
            server.Services,
            founderId,
            slots:
            [
                new WeeklyMenuSlotValues("Monday", "Lunch", [deletedRecipeId, remainingRecipeId]),
                new WeeklyMenuSlotValues("Tuesday", "Dinner", [deletedRecipeId]),
            ]);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/recipes/{deletedRecipeId}", csrf);
        var menuRecipeIds = await RecipesTestData.MenuRecipeIdsAsync(server.Services, menuId);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await RecipesTestData.RecipeExistsAsync(server.Services, deletedRecipeId));
        Assert.Equal([remainingRecipeId], menuRecipeIds);
    }

    private static WeeklyMenuSlotResponse Slot(WeeklyMenuResponse menu, string day, string slot) =>
        menu.Slots.Single(candidate => candidate.Day == day && candidate.Slot == slot);

    private sealed record ProblemPayload(string? Code);
}
