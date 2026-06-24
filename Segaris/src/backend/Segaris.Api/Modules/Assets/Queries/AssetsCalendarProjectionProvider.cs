using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Queries;

/// <summary>
/// Publishes expected end-of-life dates for accessible assets that are not
/// <see cref="AssetStatus.Retired"/> and carry an expected end-of-life date inside the
/// requested range. Unlike the Assets launcher-attention rule, Calendar is a queryable
/// time view, so past and overdue dates inside the range are included.
/// </summary>
internal sealed class AssetsCalendarProjectionProvider(SegarisDbContext database)
    : IAssetsCalendarProjectionProvider
{
    public async Task<IReadOnlyList<AssetExpectedEndOfLifeCalendarProjection>> ListCalendarExpectedEndOfLifeAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        return await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(viewer))
            .Where(asset => asset.Status != AssetStatus.Retired)
            .Where(asset => asset.ExpectedEndOfLifeDate != null
                && asset.ExpectedEndOfLifeDate >= from
                && asset.ExpectedEndOfLifeDate <= to)
            .Select(asset => new AssetExpectedEndOfLifeCalendarProjection(
                asset.Id,
                asset.Name,
                asset.Status.ToString(),
                asset.ExpectedEndOfLifeDate!.Value,
                $"/assets?assetId={asset.Id}"))
            .ToArrayAsync(cancellationToken);
    }
}
