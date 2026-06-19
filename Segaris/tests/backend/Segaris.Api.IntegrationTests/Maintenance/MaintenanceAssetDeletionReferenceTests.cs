using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Assets;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Maintenance;

public sealed class MaintenanceAssetDeletionReferenceTests
{
    [Fact]
    public async Task Delete_blocks_referenced_assets_and_reports_privacy_neutral_impact()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var sourceAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Referenced boiler",
            visibility: RecordVisibility.Public);
        await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Service boiler",
            assetId: sourceAssetId);

        var impact = await client.GetFromJsonAsync<AssetDeletionImpactResponse>(
            $"/api/assets/items/{sourceAssetId}/deletion-impact",
            CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/assets/items/{sourceAssetId}", csrf);
        var problem = await deleted.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.NotNull(impact);
        Assert.True(impact.IsReferenced);
        Assert.Equal(1, impact.ReferenceCount);
        Assert.False(impact.CanDeleteDirectly);
        Assert.True(impact.RequiresReassignment);
        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        Assert.Equal("assets.asset.deletion_referenced", problem!.Code);
    }

    [Fact]
    public async Task Reassign_and_delete_moves_mixed_status_and_ownership_tasks_atomically()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("asset-reassign-owner", "AssetReassignOwner123!");
        var sourceAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Old shared appliance",
            visibility: RecordVisibility.Public);
        var targetAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "New shared appliance",
            visibility: RecordVisibility.Public);
        var founderTaskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Pending founder task",
            status: MaintenanceStatus.Pending,
            assetId: sourceAssetId,
            visibility: RecordVisibility.Public);
        var memberTaskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            memberId,
            title: "Completed member task",
            status: MaintenanceStatus.Completed,
            assetId: sourceAssetId,
            visibility: RecordVisibility.Public);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/assets/items/{sourceAssetId}/reassign-and-delete",
            new AssetReassignmentDeletionRequest(targetAssetId),
            csrf);

        var founderTask = await client.GetFromJsonAsync<MaintenanceTaskResponse>(
            $"/api/maintenance/tasks/{founderTaskId}",
            CancellationToken.None);
        var memberTask = await client.GetFromJsonAsync<MaintenanceTaskResponse>(
            $"/api/maintenance/tasks/{memberTaskId}",
            CancellationToken.None);
        using var sourceFetch = await client.GetAsync($"/api/assets/items/{sourceAssetId}", CancellationToken.None);
        using var targetFetch = await client.GetAsync($"/api/assets/items/{targetAssetId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sourceFetch.StatusCode);
        Assert.Equal(HttpStatusCode.OK, targetFetch.StatusCode);
        Assert.Equal(targetAssetId, founderTask!.AssetId);
        Assert.Equal("New shared appliance", founderTask.AssetName);
        Assert.Equal(targetAssetId, memberTask!.AssetId);
        Assert.Equal("New shared appliance", memberTask.AssetName);
        Assert.Equal("Completed", memberTask.Status);
    }

    [Fact]
    public async Task Reassign_and_delete_rolls_back_when_target_violates_visibility_for_any_task()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var sourceAssetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Public source",
            visibility: RecordVisibility.Public);
        var privateTargetId = await AssetsTestData.SeedAssetAsync(
            server.Services,
            founderId,
            name: "Private target",
            visibility: RecordVisibility.Private);
        var taskId = await MaintenanceTestData.SeedTaskAsync(
            server.Services,
            founderId,
            title: "Public task",
            assetId: sourceAssetId,
            visibility: RecordVisibility.Public);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/assets/items/{sourceAssetId}/reassign-and-delete",
            new AssetReassignmentDeletionRequest(privateTargetId),
            csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var task = await client.GetFromJsonAsync<MaintenanceTaskResponse>(
            $"/api/maintenance/tasks/{taskId}",
            CancellationToken.None);
        using var sourceFetch = await client.GetAsync($"/api/assets/items/{sourceAssetId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("maintenance.asset_deletion.blocked", problem!.Code);
        Assert.Equal(HttpStatusCode.OK, sourceFetch.StatusCode);
        Assert.Equal(sourceAssetId, task!.AssetId);
        Assert.Equal("Public source", task.AssetName);
    }

    private sealed record ProblemPayload(string? Code);
}
