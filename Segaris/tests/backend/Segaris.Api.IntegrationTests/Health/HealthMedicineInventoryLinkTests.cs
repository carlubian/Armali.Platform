using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.IntegrationTests.Inventory;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Health;

public sealed class HealthMedicineInventoryLinkTests
{
    [Fact]
    public async Task Create_update_and_detail_resolve_and_clear_inventory_item_link()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Ibuprofen box");
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Analgesic");

        using var createdResponse = await CapexApi.PostJsonAsync(
            client,
            "/api/health/medicines",
            new CreateMedicineRequest("Ibuprofen", categoryId, "With food", false, itemId, null, "Public"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<MedicineResponse>(CancellationToken.None);

        using var updatedResponse = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/medicines/{created!.Id}",
            new UpdateMedicineRequest("Ibuprofen", categoryId, "With food", false, null, null, "Public"),
            csrf);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<MedicineResponse>(CancellationToken.None);

        Assert.Equal(itemId, created.InventoryItemId);
        Assert.Equal("Ibuprofen box", created.InventoryItemName);
        Assert.Null(updated!.InventoryItemId);
        Assert.Null(updated.InventoryItemName);
    }

    [Fact]
    public async Task Medicine_item_references_enforce_accessibility_and_visibility()
    {
        using var server = new CapexTestServer();
        using var ownerClient = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(ownerClient);
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            ownerId,
            name: "Private antibiotic",
            visibility: RecordVisibility.Private);
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Other");

        using var publicMedicine = await CapexApi.PostJsonAsync(
            ownerClient,
            "/api/health/medicines",
            new CreateMedicineRequest("Antibiotic", categoryId, null, true, privateItemId, null, "Public"),
            ownerCsrf);
        var publicProblem = await publicMedicine.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        await server.CreateUserAsync("health-wave6-member", "HealthWave6Member123!");
        using var memberClient = server.CreateClient();
        await CapexTestServer.LoginAsync(memberClient, "health-wave6-member", "HealthWave6Member123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(memberClient);
        using var inaccessibleItem = await CapexApi.PostJsonAsync(
            memberClient,
            "/api/health/medicines",
            new CreateMedicineRequest("Antibiotic", categoryId, null, true, privateItemId, null, "Private"),
            memberCsrf);
        var inaccessibleProblem = await inaccessibleItem.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, publicMedicine.StatusCode);
        Assert.Equal("health.medicine.item_visibility_forbidden", publicProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, inaccessibleItem.StatusCode);
        Assert.Equal("health.medicine.item_not_accessible", inaccessibleProblem!.Code);
    }

    [Fact]
    public async Task Visibility_and_item_changes_cannot_make_a_public_medicine_reference_a_private_item()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Private antihistamine",
            visibility: RecordVisibility.Private);
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Other");
        var privateMedicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Private medicine",
            inventoryItemId: privateItemId,
            visibility: RecordVisibility.Private);
        var publicMedicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Public medicine",
            visibility: RecordVisibility.Public);

        using var makePublic = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/medicines/{privateMedicineId}",
            new UpdateMedicineRequest("Private medicine", categoryId, null, false, privateItemId, null, "Public"),
            csrf);
        using var addPrivateItem = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/medicines/{publicMedicineId}",
            new UpdateMedicineRequest("Public medicine", categoryId, null, false, privateItemId, null, "Public"),
            csrf);

        var makePublicProblem = await makePublic.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var addPrivateItemProblem = await addPrivateItem.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Forbidden, makePublic.StatusCode);
        Assert.Equal("health.medicine.item_visibility_forbidden", makePublicProblem!.Code);
        Assert.Equal(HttpStatusCode.Forbidden, addPrivateItem.StatusCode);
        Assert.Equal("health.medicine.item_visibility_forbidden", addPrivateItemProblem!.Code);
    }

    [Fact]
    public async Task Public_medicine_detail_does_not_resolve_private_item_names_for_other_users()
    {
        using var server = new CapexTestServer();
        using var ownerClient = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(ownerClient);
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, ownerId, name: "Vitamin D bottle");
        var categoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Supplement");

        using var createdResponse = await CapexApi.PostJsonAsync(
            ownerClient,
            "/api/health/medicines",
            new CreateMedicineRequest("Vitamin D", categoryId, null, false, itemId, null, "Public"),
            ownerCsrf);
        createdResponse.EnsureSuccessStatusCode();
        var created = await createdResponse.Content.ReadFromJsonAsync<MedicineResponse>(CancellationToken.None);

        var privateItemUpdate = (await InventoryItemMutationTests.DefaultBuilderAsync(server))
            .WithName("Vitamin D bottle")
            .WithStatus("Candidate")
            .WithVisibility("Private")
            .BuildUpdate();
        using var itemUpdated = await CapexApi.PutJsonAsync(ownerClient, $"/api/inventory/items/{itemId}", privateItemUpdate, ownerCsrf);
        itemUpdated.EnsureSuccessStatusCode();

        await server.CreateUserAsync("health-wave6-viewer", "HealthWave6Viewer123!");
        using var viewerClient = server.CreateClient();
        await CapexTestServer.LoginAsync(viewerClient, "health-wave6-viewer", "HealthWave6Viewer123!");

        var detail = await viewerClient.GetFromJsonAsync<MedicineResponse>($"/api/health/medicines/{created!.Id}", CancellationToken.None);

        Assert.Equal(itemId, detail!.InventoryItemId);
        Assert.Null(detail.InventoryItemName);
    }

    private sealed record ProblemPayload(string? Code);
}
