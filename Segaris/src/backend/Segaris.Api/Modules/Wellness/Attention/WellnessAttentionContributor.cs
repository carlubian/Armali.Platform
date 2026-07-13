using Segaris.Api.Modules.Launcher.Contracts;

namespace Segaris.Api.Modules.Wellness.Attention;

internal sealed class WellnessAttentionContributor : ILauncherAttentionContributor
{
    public string Module => "wellness";

    public Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
