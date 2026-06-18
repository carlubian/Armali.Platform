using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Clothes.Domain;

/// <summary>
/// Visibility rules for Clothes garments. A garment is accessible when it is
/// public or was created by the requesting user; private garments remain
/// creator-only, including from administrators.
/// </summary>
internal static class ClothesGarmentPolicies
{
    public static Expression<Func<ClothesGarment, bool>> AccessibleTo(UserId userId) =>
        garment => garment.Visibility == RecordVisibility.Public || garment.CreatedBy == userId.Value;

    public static Expression<Func<ClothesGarment, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(ClothesGarment garment, UserId userId) =>
        garment.CreatedBy == userId.Value;
}
