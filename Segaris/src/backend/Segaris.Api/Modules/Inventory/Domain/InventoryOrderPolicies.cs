using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>
/// Visibility rules for Inventory orders. Orders follow the same baseline privacy
/// model as items: public records are collaborative and private records are
/// creator-only, including from administrators.
/// </summary>
internal static class InventoryOrderPolicies
{
    public static Expression<Func<InventoryOrder, bool>> AccessibleTo(UserId userId) =>
        order => order.Visibility == RecordVisibility.Public || order.CreatedBy == userId.Value;

    public static Expression<Func<InventoryOrder, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(InventoryOrder order, UserId userId) =>
        order.CreatedBy == userId.Value;
}
