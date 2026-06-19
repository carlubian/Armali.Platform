using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Domain;

/// <summary>
/// Visibility rules for assets. An asset is accessible when it is public or created
/// by the requesting user; a private asset remains creator-only, including from
/// administrators. Public records collaborate: any authenticated user may edit a
/// public asset, while only the creator may change its visibility (enforced in the
/// domain). Mutation accessibility therefore mirrors read accessibility.
/// </summary>
internal static class AssetPolicies
{
    public static Expression<Func<Asset, bool>> AccessibleTo(UserId userId) =>
        asset => asset.Visibility == RecordVisibility.Public || asset.CreatedBy == userId.Value;

    public static Expression<Func<Asset, bool>> MutableBy(UserId userId) => AccessibleTo(userId);
}
