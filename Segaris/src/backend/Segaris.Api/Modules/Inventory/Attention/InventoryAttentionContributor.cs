using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Attention;

/// <summary>
/// Contributes the Inventory launcher card's attention state. Attention is required
/// when the current user can access at least one <c>Active</c> item whose
/// <c>CurrentStock</c> is less than or equal to its <c>MinimumStock</c>. The same
/// visibility rules as the read APIs apply, so administrators receive no privacy
/// bypass, and <c>Candidate</c> and <c>Deprecated</c> items never activate attention.
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
                item => item.Status == InventoryItemStatus.Active && item.CurrentStock <= item.MinimumStock,
                cancellationToken);
    }
}
