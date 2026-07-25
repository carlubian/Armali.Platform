using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Attention;

/// <summary>
/// Contributes the Inventory launcher card's attention state. Attention is required
/// when the current user can access at least one item that satisfies all three
/// conditions: its status is <c>Active</c>, its <c>MinimumStock</c> is greater than
/// zero, and its <c>CurrentStock</c> is less than or equal to its <c>MinimumStock</c>.
/// These are exactly the shopping list inclusion conditions, so attention is true when
/// the current user's shopping list is not empty. The same visibility rules as the read
/// APIs apply, so administrators receive no privacy bypass, <c>Candidate</c> and
/// <c>Deprecated</c> items never activate attention, and neither do items that track no
/// replenishment threshold.
/// </summary>
internal sealed class InventoryAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser) : ILauncherAttentionContributor
{
    public string Module => InventoryLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        return await database.Set<InventoryItem>()
            .AsNoTracking()
            .Where(InventoryItemPolicies.AccessibleTo(userId))
            .AnyAsync(
                item => item.Status == InventoryItemStatus.Active
                    && item.MinimumStock > 0m
                    && item.CurrentStock <= item.MinimumStock,
                cancellationToken);
    }
}
