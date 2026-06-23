using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Health;

public sealed class HealthMedicineEndpointTests
{
    [Fact]
    public async Task Medicines_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/health/medicines", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_and_optional_fields()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Analgesic");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/health/medicines",
            new CreateMedicineRequest("  Ibuprofen  ", categoryId, null, null, null, null, null),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MedicineResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Ibuprofen", created.Name);
        Assert.Equal(categoryId, created.CategoryId);
        Assert.Equal("Analgesic", created.CategoryName);
        Assert.Null(created.Posology);
        Assert.False(created.RequiresPrescription);
        Assert.Null(created.InventoryItemId);
        Assert.Null(created.InventoryItemName);
        Assert.Null(created.Notes);
        Assert.Empty(created.Attachments);
        Assert.Equal("Public", created.Visibility);
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_and_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Ibuprofen",
            categoryName: "Analgesic",
            posology: "With food");
        await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Amoxicillin",
            categoryName: "Antibiotic",
            requiresPrescription: true);
        await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Cetirizine",
            categoryName: "Antihistamine",
            visibility: RecordVisibility.Private);

        var antibioticId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Antibiotic");

        var firstPage = await GetPageAsync(client, "/api/health/medicines?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/health/medicines?search=MOX");
        var byCategory = await GetPageAsync(client, $"/api/health/medicines?category={antibioticId}");
        var prescriptionOnly = await GetPageAsync(client, "/api/health/medicines?requiresPrescription=true");
        var privateOnly = await GetPageAsync(client, "/api/health/medicines?visibility=Private");
        var byCategorySort = await GetPageAsync(client, "/api/health/medicines?pageSize=10&sort=category&sortDirection=asc");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal("Amoxicillin", Assert.Single(search.Items).Name);
        Assert.Equal("Amoxicillin", Assert.Single(byCategory.Items).Name);
        Assert.Equal("Amoxicillin", Assert.Single(prescriptionOnly.Items).Name);
        Assert.Equal("Cetirizine", Assert.Single(privateOnly.Items).Name);
        Assert.Equal(["Analgesic", "Antibiotic", "Antihistamine"], byCategorySort.Items.Select(item => item.CategoryName).ToArray());
    }

    [Fact]
    public async Task Detail_update_and_delete_manage_the_complete_medicine()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Original", posology: "Old");
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Supplement");

        var detail = await client.GetFromJsonAsync<MedicineResponse>($"/api/health/medicines/{medicineId}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/medicines/{medicineId}",
            new UpdateMedicineRequest("Vitamin D", categoryId, "Daily", true, null, "Winter", "Private"),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<MedicineResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/health/medicines/{medicineId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Name);
        Assert.Equal("Old", detail.Posology);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Vitamin D", updated!.Name);
        Assert.Equal(categoryId, updated.CategoryId);
        Assert.Equal("Daily", updated.Posology);
        Assert.True(updated.RequiresPrescription);
        Assert.Null(updated.InventoryItemId);
        Assert.Null(updated.InventoryItemName);
        Assert.Equal("Winter", updated.Notes);
        Assert.Equal("Private", updated.Visibility);
        Assert.Empty(updated.Attachments);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await HealthTestData.MedicineExistsAsync(server.Services, medicineId));
    }

    [Fact]
    public async Task Unknown_references_and_invalid_values_return_medicine_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Analgesic");

        using var unknownCategory = await CapexApi.PostJsonAsync(
            client,
            "/api/health/medicines",
            new CreateMedicineRequest("Ibuprofen", 999_999, null, null, null, null, null),
            csrf);
        using var invalidName = await CapexApi.PostJsonAsync(
            client,
            "/api/health/medicines",
            new CreateMedicineRequest("   ", categoryId, null, null, null, null, null),
            csrf);

        var unknownProblem = await unknownCategory.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidNameProblem = await invalidName.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknownCategory.StatusCode);
        Assert.Equal("health.catalog.unknown_reference", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidName.StatusCode);
        Assert.Equal("health.medicine.validation", invalidNameProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_private_isolation_and_not_found_privacy_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicMedicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Shared",
            visibility: RecordVisibility.Public);
        var privateMedicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Other");

        await server.CreateUserAsync("health-medicine-member", "HealthMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "health-medicine-member", "HealthMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var privateDetail = await member.GetAsync($"/api/health/medicines/{privateMedicineId}", CancellationToken.None);
        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/health/medicines/{publicMedicineId}",
            new UpdateMedicineRequest("Shared edited", categoryId, null, false, null, null, null),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/health/medicines/{privateMedicineId}",
            new UpdateMedicineRequest("Private edited", categoryId, null, false, null, null, "Private"),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/health/medicines/{publicMedicineId}",
            new UpdateMedicineRequest("Shared hidden", categoryId, null, false, null, null, "Private"),
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, privateDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
    }

    private static async Task<PaginatedResponse<MedicineSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<MedicineSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}
