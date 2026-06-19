using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Assets;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Maintenance;

public sealed class MaintenanceTaskWave3Tests
{
    [Fact]
    public async Task Create_update_detail_and_list_resolve_linked_assets_and_support_asset_filter()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var typeId = await MaintenanceTestData.TypeIdAsync(server.Services, "Repair");
        var linkedAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Boiler",
            visibility: RecordVisibility.Public);
        var otherAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Garden gate",
            visibility: RecordVisibility.Public);

        using var create = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default()
                .WithTitle("Inspect boiler")
                .WithType(typeId)
                .WithAsset(linkedAssetId)
                .BuildCreate(),
            csrf);
        var created = await create.Content.ReadFromJsonAsync<MaintenanceTaskResponse>(CancellationToken.None);
        var filteredToLinked = await GetPageAsync(client, $"/api/maintenance/tasks?asset={linkedAssetId}");
        var filteredToOther = await GetPageAsync(client, $"/api/maintenance/tasks?asset={otherAssetId}");
        var detail = await client.GetFromJsonAsync<MaintenanceTaskResponse>(
            $"/api/maintenance/tasks/{created!.Id}",
            CancellationToken.None);

        using var unlink = await CapexApi.PutJsonAsync(
            client,
            $"/api/maintenance/tasks/{created.Id}",
            MaintenanceTaskRequestBuilder.Default()
                .WithTitle("Inspect boiler")
                .WithType(typeId)
                .WithAsset(null)
                .BuildUpdate(),
            csrf);
        var unlinked = await unlink.Content.ReadFromJsonAsync<MaintenanceTaskResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(linkedAssetId, created.AssetId);
        Assert.Equal("Boiler", created.AssetName);
        Assert.Equal("Inspect boiler", Assert.Single(filteredToLinked.Items).Title);
        Assert.Empty(filteredToOther.Items);
        Assert.Equal("Boiler", detail!.AssetName);
        Assert.Equal(HttpStatusCode.OK, unlink.StatusCode);
        Assert.Null(unlinked!.AssetId);
        Assert.Null(unlinked.AssetName);
    }

    [Fact]
    public async Task Writes_enforce_asset_accessibility_and_task_asset_visibility_compatibility()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("maintenance-owner", "MaintenanceOwner123!");
        var typeId = await MaintenanceTestData.TypeIdAsync(server.Services, "Repair");
        var founderPrivateAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Founder private asset",
            visibility: RecordVisibility.Private);
        var memberPrivateAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            memberId,
            name: "Member private asset",
            visibility: RecordVisibility.Private);

        using var publicToPrivate = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default()
                .WithType(typeId)
                .WithAsset(founderPrivateAssetId)
                .WithVisibility("Public")
                .BuildCreate(),
            csrf);
        using var privateToOwnPrivate = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default()
                .WithType(typeId)
                .WithAsset(founderPrivateAssetId)
                .WithVisibility("Private")
                .BuildCreate(),
            csrf);
        using var privateToInaccessible = await CapexApi.PostJsonAsync(
            client,
            "/api/maintenance/tasks",
            MaintenanceTaskRequestBuilder.Default()
                .WithType(typeId)
                .WithAsset(memberPrivateAssetId)
                .WithVisibility("Private")
                .BuildCreate(),
            csrf);

        var publicProblem = await publicToPrivate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var inaccessibleProblem = await privateToInaccessible.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var linkedPrivateTask = await privateToOwnPrivate.Content.ReadFromJsonAsync<MaintenanceTaskResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, publicToPrivate.StatusCode);
        Assert.Equal("maintenance.task.asset_visibility_forbidden", publicProblem!.Code);
        Assert.Equal(HttpStatusCode.Created, privateToOwnPrivate.StatusCode);
        Assert.Equal(founderPrivateAssetId, linkedPrivateTask!.AssetId);
        Assert.Equal("Founder private asset", linkedPrivateTask.AssetName);
        Assert.Equal(HttpStatusCode.BadRequest, privateToInaccessible.StatusCode);
        Assert.Equal("maintenance.task.asset_invalid", inaccessibleProblem!.Code);
    }

    [Fact]
    public async Task Public_tasks_do_not_expose_private_asset_names_to_other_users()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Hidden asset name",
            visibility: RecordVisibility.Private);
        var taskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Legacy public link",
            assetId: privateAssetId,
            visibility: RecordVisibility.Public);

        await server.CreateUserAsync("maintenance-viewer", "MaintenanceViewer123!");
        using var viewer = server.CreateClient();
        await CapexTestServer.LoginAsync(viewer, "maintenance-viewer", "MaintenanceViewer123!");

        var page = await GetPageAsync(viewer, "/api/maintenance/tasks");
        var detail = await viewer.GetFromJsonAsync<MaintenanceTaskResponse>(
            $"/api/maintenance/tasks/{taskId}",
            CancellationToken.None);

        var row = Assert.Single(page.Items);
        Assert.Equal("Legacy public link", row.Title);
        Assert.Equal(privateAssetId, row.AssetId);
        Assert.Null(row.AssetName);
        Assert.NotNull(detail);
        Assert.Equal(privateAssetId, detail.AssetId);
        Assert.Null(detail.AssetName);
    }

    private static async Task<PaginatedResponse<MaintenanceTaskSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<MaintenanceTaskSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}
