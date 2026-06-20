using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Projects.Contracts;

/// <summary>The structural node kinds Projects manages through the Configuration experience.</summary>
internal enum ProjectsStructuralNodeKind
{
    Program,
    Axis,
}

/// <summary>
/// Frozen description of a Projects-owned structural node managed through Configuration.
/// <see cref="HasParent"/> is true for an axis, which references a parent program.
/// </summary>
internal sealed record ProjectsStructuralNodeDescriptor(
    ProjectsStructuralNodeKind Kind,
    string RouteSegment,
    bool HasParent);

/// <summary>
/// The Configuration-facing contracts owned by Projects. Programs and axes are
/// module-owned structural nodes — not flat Configuration catalogues — so Projects
/// consumes no shared Configuration reference kinds and instead exposes its own managed
/// nodes for the Configuration presentation boundary. Deleting a non-empty node requires
/// atomic reassignment of all children to a single compatible target; Configuration
/// never queries Projects' tables directly.
/// </summary>
internal static class ProjectsConfigurationContracts
{
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<ProjectsStructuralNodeDescriptor> ManagedNodes =
    [
        new(ProjectsStructuralNodeKind.Program, "programs", HasParent: false),
        new(ProjectsStructuralNodeKind.Axis, "axes", HasParent: true),
    ];
}
