using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Domain;

/// <summary>
/// Visibility rules for Health diseases. A disease is accessible when it is public
/// or was created by the requesting user; private diseases remain creator-only.
/// </summary>
internal static class HealthDiseasePolicies
{
    public static Expression<Func<Disease, bool>> AccessibleTo(UserId userId) =>
        disease => disease.Visibility == RecordVisibility.Public || disease.CreatedBy == userId.Value;

    public static Expression<Func<Disease, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(Disease disease, UserId userId) =>
        disease.CreatedBy == userId.Value;
}
