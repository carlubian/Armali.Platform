namespace Segaris.Api.Modules.Processes;

/// <summary>
/// Frozen identity used by the Processes launcher attention contributor. The key is the
/// stable module identifier reported in the aggregated launcher attention response.
/// Wave 5 implements the overdue-and-upcoming attention rule; until then the contributor
/// reports a constant non-attention state.
/// </summary>
internal static class ProcessesLauncherCard
{
    public const string ModuleKey = "processes";
}
