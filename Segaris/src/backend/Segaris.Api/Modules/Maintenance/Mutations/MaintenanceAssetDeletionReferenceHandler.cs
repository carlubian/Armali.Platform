using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance.Mutations;

/// <summary>
/// Maintenance implementation of the Assets-owned deletion-reference contract.
/// Assets enumerates this handler through DI and never references Maintenance
/// entities directly.
/// </summary>
internal sealed class MaintenanceAssetDeletionReferenceHandler(
    SegarisDbContext database,
    IAssetReferenceReader assetReferences) : IAssetDeletionReferenceHandler
{
    public Task<int> CountReferencesAsync(int assetId, CancellationToken cancellationToken) =>
        database.Set<MaintenanceTask>()
            .AsNoTracking()
            .CountAsync(task => task.AssetId == assetId, cancellationToken);

    public async Task ReassignReferencesAsync(
        AssetDeletionReassignment reassignment,
        CancellationToken cancellationToken)
    {
        var tasks = await database.Set<MaintenanceTask>()
            .Where(task => task.AssetId == reassignment.SourceAssetId)
            .ToListAsync(cancellationToken);
        if (tasks.Count == 0)
        {
            return;
        }

        await EnsureTargetCompatibleAsync(tasks, reassignment.TargetAssetId, cancellationToken);
        foreach (var task in tasks)
        {
            task.ReplaceAsset(reassignment.TargetAssetId, reassignment.Actor, reassignment.OccurredAt);
        }
    }

    private async Task EnsureTargetCompatibleAsync(
        IReadOnlyCollection<MaintenanceTask> tasks,
        int targetAssetId,
        CancellationToken cancellationToken)
    {
        var creatorIds = tasks
            .Select(task => task.CreatedBy)
            .Distinct()
            .ToArray();

        foreach (var creatorId in creatorIds)
        {
            var asset = await assetReferences.FindAccessibleAsync(
                targetAssetId,
                new UserId(creatorId),
                cancellationToken);
            if (asset is null)
            {
                throw Blocked();
            }

            if (asset.Visibility != RecordVisibility.Public
                && tasks.Any(task => task.CreatedBy == creatorId && task.Visibility == RecordVisibility.Public))
            {
                throw Blocked();
            }
        }
    }

    private static AssetReassignmentBlockedException Blocked() => new(
        MaintenanceErrorCodes.AssetDeletionBlocked,
        "The target asset cannot receive every maintenance task reference.");
}
