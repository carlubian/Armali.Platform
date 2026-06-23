using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Domain;

/// <summary>
/// Visibility rules for Health medicines. A medicine is accessible when it is public
/// or was created by the requesting user; private medicines remain creator-only.
/// </summary>
internal static class HealthMedicinePolicies
{
    public static Expression<Func<Medicine, bool>> AccessibleTo(UserId userId) =>
        medicine => medicine.Visibility == RecordVisibility.Public || medicine.CreatedBy == userId.Value;

    public static Expression<Func<Medicine, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(Medicine medicine, UserId userId) =>
        medicine.CreatedBy == userId.Value;
}
