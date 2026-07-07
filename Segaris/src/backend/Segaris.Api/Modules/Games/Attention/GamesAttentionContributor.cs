using Segaris.Api.Modules.Launcher.Contracts;

namespace Segaris.Api.Modules.Games.Attention;

internal sealed class GamesAttentionContributor : ILauncherAttentionContributor
{
    public string Module => "games";

    public Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
