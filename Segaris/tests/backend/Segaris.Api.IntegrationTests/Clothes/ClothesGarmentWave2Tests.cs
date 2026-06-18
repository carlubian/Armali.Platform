using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Clothes;

public sealed class ClothesGarmentWave2Tests
{
    [Fact]
    public async Task Garments_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/clothes/garments", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_colours_and_care_values()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await ClothesTestData.CategoryIdAsync(server.Services, "Tops");
        var blackId = await ClothesTestData.ColorIdAsync(server.Services, "Black");
        var whiteId = await ClothesTestData.ColorIdAsync(server.Services, "White");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/clothes/garments",
            ClothesGarmentRequestBuilder.Default()
                .WithName("  Linen shirt  ")
                .WithCategory(categoryId)
                .WithStatus(null)
                .WithColors(blackId, blackId, whiteId)
                .WithCare(washing: "Wash30", drying: "Delicate", ironing: "Low", dryCleaning: "DoNotDryClean")
                .WithVisibility(null)
                .BuildCreate(),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClothesGarmentResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Linen shirt", created.Name);
        Assert.Equal("Active", created.Status);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal(["Black", "White"], created.Colors.Select(color => color.Name).ToArray());
        Assert.Equal("Wash30", created.WashingCare);
        Assert.Equal("Delicate", created.DryingCare);
        Assert.Equal("Low", created.IroningCare);
        Assert.Equal("DoNotDryClean", created.DryCleaningCare);
        Assert.Equal("placeholder", created.Thumbnail.Source);
        Assert.Empty(created.Attachments);
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_and_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await ClothesTestData.SeedGarmentAsync(
            server.Services,
            founderId,
            name: "Blue jeans",
            categoryName: "Bottoms",
            status: ClothesGarmentStatus.Active,
            size: "32",
            colorNames: ["Blue"],
            washingCare: WashingCare.Wash40,
            ironingCare: IroningCare.Low);
        await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Red scarf", categoryName: "Accessories", status: ClothesGarmentStatus.Unavailable, notes: "winter wool", colorNames: ["Red"]);
        await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Black coat", categoryName: "Outerwear", status: ClothesGarmentStatus.Deprecated, colorNames: ["Black"], visibility: RecordVisibility.Private);

        var redId = await ClothesTestData.ColorIdAsync(server.Services, "Red");
        var accessoriesId = await ClothesTestData.CategoryIdAsync(server.Services, "Accessories");

        var firstPage = await GetPageAsync(client, "/api/clothes/garments?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/clothes/garments?search=WOOL");
        var byCategory = await GetPageAsync(client, $"/api/clothes/garments?category={accessoriesId}");
        var byColor = await GetPageAsync(client, $"/api/clothes/garments?color={redId}");
        var privateOnly = await GetPageAsync(client, "/api/clothes/garments?visibility=Private");
        var byStatus = await GetPageAsync(client, "/api/clothes/garments?status=Deprecated");
        var byCategorySort = await GetPageAsync(client, "/api/clothes/garments?sort=category&sortDirection=asc");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        var blueJeans = firstPage.Items.Single(item => item.Name == "Blue jeans");
        Assert.Equal("Wash40", blueJeans.WashingCare);
        Assert.Null(blueJeans.DryingCare);
        Assert.Equal("Low", blueJeans.IroningCare);
        Assert.Null(blueJeans.DryCleaningCare);
        Assert.Equal("Red scarf", Assert.Single(search.Items).Name);
        Assert.Equal("Red scarf", Assert.Single(byCategory.Items).Name);
        Assert.Equal("Red scarf", Assert.Single(byColor.Items).Name);
        Assert.Equal("Black coat", Assert.Single(privateOnly.Items).Name);
        Assert.Equal("Black coat", Assert.Single(byStatus.Items).Name);
        Assert.Equal(["Accessories", "Bottoms", "Outerwear"], byCategorySort.Items.Select(item => item.CategoryName).ToArray());
    }

    [Fact]
    public async Task Detail_update_and_delete_manage_the_complete_garment()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var garmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Original", colorNames: ["Blue"]);
        var categoryId = await ClothesTestData.CategoryIdAsync(server.Services, "Outerwear");
        var blackId = await ClothesTestData.ColorIdAsync(server.Services, "Black");
        var whiteId = await ClothesTestData.ColorIdAsync(server.Services, "White");

        var detail = await client.GetFromJsonAsync<ClothesGarmentResponse>($"/api/clothes/garments/{garmentId}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/clothes/garments/{garmentId}",
            ClothesGarmentRequestBuilder.Default()
                .WithName("Updated coat")
                .WithCategory(categoryId)
                .WithStatus("Unavailable")
                .WithSize("L")
                .WithColors(blackId, whiteId, whiteId)
                .WithCare(washing: "DoNotWash")
                .WithNotes("Hang dry")
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<ClothesGarmentResponse>(CancellationToken.None);
        var updatedColorIds = await ClothesTestData.GarmentColorIdsAsync(server.Services, garmentId);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/clothes/garments/{garmentId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Name);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated coat", updated!.Name);
        Assert.Equal("Unavailable", updated.Status);
        Assert.Equal("L", updated.Size);
        Assert.Equal(["Black", "White"], updated.Colors.Select(color => color.Name).ToArray());
        Assert.Equal([blackId, whiteId], updatedColorIds);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await ClothesTestData.GarmentExistsAsync(server.Services, garmentId));
    }

    [Fact]
    public async Task Unknown_references_and_invalid_values_return_garment_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await ClothesTestData.CategoryIdAsync(server.Services, "Tops");

        using var unknown = await CapexApi.PostJsonAsync(
            client,
            "/api/clothes/garments",
            ClothesGarmentRequestBuilder.Default().WithCategory(categoryId).WithColors(999_999).BuildCreate(),
            csrf);
        using var invalid = await CapexApi.PostJsonAsync(
            client,
            "/api/clothes/garments",
            ClothesGarmentRequestBuilder.Default().WithCategory(categoryId).WithStatus("Nope").BuildCreate(),
            csrf);

        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("clothes.catalog.unknown_reference", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("clothes.garment.validation", invalidProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_and_private_isolation_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicGarmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Shared", visibility: RecordVisibility.Public);
        var privateGarmentId = await ClothesTestData.SeedGarmentAsync(server.Services, founderId, name: "Private", visibility: RecordVisibility.Private);
        var categoryId = await ClothesTestData.CategoryIdAsync(server.Services, "Other");

        await server.CreateUserAsync("clothes-member", "ClothesMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "clothes-member", "ClothesMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/clothes/garments/{publicGarmentId}",
            ClothesGarmentRequestBuilder.Default().WithName("Shared edited").WithCategory(categoryId).BuildUpdate(),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/clothes/garments/{privateGarmentId}",
            ClothesGarmentRequestBuilder.Default().WithName("Private edited").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/clothes/garments/{publicGarmentId}",
            ClothesGarmentRequestBuilder.Default().WithName("Shared hidden").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
    }

    private static async Task<PaginatedResponse<ClothesGarmentSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<ClothesGarmentSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}
