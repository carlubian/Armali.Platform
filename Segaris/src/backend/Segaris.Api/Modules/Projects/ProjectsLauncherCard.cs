namespace Segaris.Api.Modules.Projects;

/// <summary>
/// Frozen identity used by the Projects launcher attention contributor. The key is the
/// stable module identifier reported in the aggregated launcher attention response.
/// Projects never requests attention: the contributor reports a constant
/// non-attention state in every release of this module version.
/// </summary>
internal static class ProjectsLauncherCard
{
    public const string ModuleKey = "projects";
}
