using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Games.Domain;

/// <summary>
/// Standard Segaris visibility policy for playthroughs: public playthroughs are
/// collaboratively accessible and mutable, private ones stay creator-only. Only the
/// creator may change visibility, which is enforced separately in the domain.
/// </summary>
internal static class PlaythroughPolicies
{
    public static Expression<Func<Playthrough, bool>> AccessibleTo(UserId userId) =>
        playthrough => playthrough.Visibility == RecordVisibility.Public || playthrough.CreatedBy == userId.Value;

    public static Expression<Func<Playthrough, bool>> MutableBy(UserId userId) => AccessibleTo(userId);
}
