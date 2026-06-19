using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Assets.Attention;

/// <summary>
/// Contributes the Assets launcher card's attention state. Attention is required
/// when the current user can access at least one non-retired asset whose expected
/// end of life falls from today through the next 30 natural days in Europe/Madrid.
/// </summary>
internal sealed class AssetsAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser,
    IClock clock) : ILauncherAttentionContributor
{
    public string Module => AssetsLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var today = AssetsCivilDate.Today(clock);
        var windowEnd = today.AddDays(30);

        return await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(userId))
            .AnyAsync(
                asset => asset.Status != AssetStatus.Retired
                    && asset.ExpectedEndOfLifeDate.HasValue
                    && asset.ExpectedEndOfLifeDate.Value >= today
                    && asset.ExpectedEndOfLifeDate.Value <= windowEnd,
                cancellationToken);
    }
}
