namespace Segaris.Api.Modules.Launcher.Contracts;

/// <summary>
/// Contract a business module implements to contribute its launcher card's
/// attention state. The Launcher module aggregates every registered contributor
/// into the launcher attention response. Adding a later module means registering
/// another contributor; it does not change the Capex contributor or the
/// aggregated contract.
///
/// Internal to <c>Segaris.Api</c>: the cross-module boundary is enforced by
/// namespace ownership and the architecture tests, not by a separate assembly.
/// </summary>
internal interface ILauncherAttentionContributor
{
    /// <summary>Stable module key reported in the aggregated response (for example <c>capex</c>).</summary>
    string Module { get; }

    /// <summary>
    /// Evaluates whether the current user has at least one item that requires
    /// attention for this module. Implementations apply the same visibility and
    /// authorization rules as the module's read APIs.
    /// </summary>
    Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken);
}
