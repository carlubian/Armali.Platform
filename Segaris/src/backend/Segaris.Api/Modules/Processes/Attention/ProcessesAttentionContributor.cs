using Segaris.Api.Modules.Launcher.Contracts;

namespace Segaris.Api.Modules.Processes.Attention;

/// <summary>
/// Contributes the Processes launcher card's attention state. Wave 0 freezes the
/// launcher key with a constant non-attention state; Wave 5 replaces this with the
/// overdue-and-upcoming rule over open, accessible processes.
/// </summary>
internal sealed class ProcessesAttentionContributor : ILauncherAttentionContributor
{
    public string Module => ProcessesLauncherCard.ModuleKey;

    public Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
