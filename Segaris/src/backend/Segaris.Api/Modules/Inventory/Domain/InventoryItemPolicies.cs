using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Domain;

/// <summary>
/// Visibility rules for Inventory items. An item is accessible when it is public
/// or was created by the requesting user; private items remain creator-only,
/// including from administrators. The same expression governs the read APIs, the
/// quick stock adjustment, and the launcher attention contributor so a private
/// item is never disclosed.
/// </summary>
internal static class InventoryItemPolicies
{
    public static Expression<Func<InventoryItem, bool>> AccessibleTo(UserId userId) =>
        item => item.Visibility == RecordVisibility.Public || item.CreatedBy == userId.Value;
}
