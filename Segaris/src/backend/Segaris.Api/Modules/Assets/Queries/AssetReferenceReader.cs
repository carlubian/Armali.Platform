using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Queries;

internal sealed class AssetReferenceReader(SegarisDbContext database) : IAssetReferenceReader
{
    public Task<AssetReference?> FindAccessibleAsync(
        int assetId,
        UserId viewer,
        CancellationToken cancellationToken) =>
        database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(viewer))
            .Where(asset => asset.Id == assetId)
            .Select(asset => new AssetReference(asset.Id, asset.Name, asset.Visibility))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, AssetReference>> ResolveAccessibleAsync(
        IReadOnlyCollection<int> assetIds,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        if (assetIds.Count == 0)
        {
            return new Dictionary<int, AssetReference>();
        }

        return await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(viewer))
            .Where(asset => assetIds.Contains(asset.Id))
            .Select(asset => new AssetReference(asset.Id, asset.Name, asset.Visibility))
            .ToDictionaryAsync(asset => asset.AssetId, cancellationToken);
    }
}
