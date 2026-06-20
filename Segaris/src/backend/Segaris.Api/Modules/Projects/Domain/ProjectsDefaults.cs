using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>
/// Frozen creation defaults and shared field limits for the Projects module. A new
/// project or activity starts <see cref="ProjectStatus.Planning"/> and
/// <see cref="RecordVisibility.Public"/> with a newly allocated global number.
/// </summary>
internal static class ProjectsDefaults
{
    /// <summary>Maximum length of a program, axis, project, or activity name.</summary>
    public const int NameMaximumLength = 200;

    /// <summary>Exact length of a program or axis code (four uppercase ASCII letters).</summary>
    public const int CodeLength = 4;

    /// <summary>Maximum length of a risk description.</summary>
    public const int RiskDescriptionMaximumLength = 1000;

    /// <summary>Inclusive lower bound of a risk probability, impact, or mitigation factor.</summary>
    public const int RiskFactorMinimum = 1;

    /// <summary>Inclusive upper bound of a risk probability, impact, or mitigation factor.</summary>
    public const int RiskFactorMaximum = 5;

    public static readonly ProjectStatus Status = ProjectStatus.Planning;
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}
