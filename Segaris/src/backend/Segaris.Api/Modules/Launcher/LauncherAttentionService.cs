using Segaris.Api.Modules.Launcher.Contracts;

namespace Segaris.Api.Modules.Launcher;

/// <summary>
/// Aggregates every registered <see cref="ILauncherAttentionContributor"/> into
/// the launcher attention response. Contributors are owned and registered by
/// their business modules; the launcher only depends on the contract, so adding a
/// later module does not change this service. Modules are reported in a stable
/// key order for deterministic responses.
/// </summary>
internal sealed class LauncherAttentionService(IEnumerable<ILauncherAttentionContributor> contributors)
{
    public async Task<LauncherAttentionResponse> GetAttentionAsync(CancellationToken cancellationToken)
    {
        var ordered = contributors.OrderBy(contributor => contributor.Module, StringComparer.Ordinal);
        var modules = new List<ModuleAttention>();
        foreach (var contributor in ordered)
        {
            var requiresAttention = await contributor.RequiresAttentionAsync(cancellationToken);
            modules.Add(new ModuleAttention(contributor.Module, requiresAttention));
        }

        return new LauncherAttentionResponse(modules);
    }
}
