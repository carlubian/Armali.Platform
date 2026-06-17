using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Travel.Attention;

internal sealed class TravelAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser,
    IClock clock) : ILauncherAttentionContributor
{
    public string Module => TravelLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var today = TravelDefaults.Today(clock.UtcNow);
        var windowEnd = today.AddDays(7);

        return await database.Set<TravelTrip>()
            .AsNoTracking()
            .Where(TravelTripPolicies.AccessibleTo(userId))
            .AnyAsync(
                trip => trip.Status == TravelTripStatus.Ongoing
                    || (trip.Status == TravelTripStatus.Planned
                        && trip.StartDate >= today
                        && trip.StartDate <= windowEnd),
                cancellationToken);
    }
}
