using Segaris.Api.Modules.Launcher.Contracts;

namespace Segaris.Api.Modules.Projects.Attention;

/// <summary>
/// Contributes the Projects launcher card's attention state. Projects never requests
/// attention in this module version, so the contributor reports a constant
/// non-attention state for every user.
/// </summary>
internal sealed class ProjectsAttentionContributor : ILauncherAttentionContributor
{
    public string Module => ProjectsLauncherCard.ModuleKey;

    public Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
