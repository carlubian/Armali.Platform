using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Capex.Attention;

/// <summary>
/// Contributes the Capex launcher card's attention state. Attention is required
/// when the current user can access at least one <c>Planning</c> entry whose
/// <c>DueDate</c> is today or earlier in the household <c>Europe/Madrid</c> civil
/// date. The same visibility rules as the read APIs apply, so administrators
/// receive no privacy bypass.
/// </summary>
internal sealed class CapexAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser,
    IClock clock) : ILauncherAttentionContributor
{
    public string Module => CapexLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var today = CapexCivilDate.Today(clock);
        return await database.Set<CapexEntry>()
            .AsNoTracking()
            .Where(CapexEntryPolicies.AccessibleTo(userId))
            .AnyAsync(
                entry => entry.Status == CapexEntryStatus.Planning && entry.DueDate <= today,
                cancellationToken);
    }
}
