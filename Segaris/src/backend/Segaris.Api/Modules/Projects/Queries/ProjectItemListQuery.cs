using Segaris.Api.Modules.Projects.Domain;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Projects.Queries;

/// <summary>
/// Bound query surface for <c>GET /api/projects/items</c>. Every filter is optional;
/// omitting all of them returns every accessible project and activity. Unknown program
/// or axis identifiers simply match nothing, while unknown status or kind names are
/// rejected.
/// </summary>
internal sealed class ProjectItemListQuery
{
    public int? ProgramId { get; init; }

    public int? AxisId { get; init; }

    public string? Status { get; init; }

    public string? Kind { get; init; }

    public ProjectItemListFilter ToFilter()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var status = ParseEnum<ProjectStatus>(Status, "status", errors);
        var kind = ParseEnum<ProjectItemKind>(Kind, "kind", errors);
        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }

        return new ProjectItemListFilter(ProgramId, AxisId, status, kind);
    }

    private static TEnum? ParseEnum<TEnum>(
        string? value,
        string parameter,
        Dictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        errors[parameter] =
            [$"'{value}' is not a valid {parameter}. Allowed values: {string.Join(", ", Enum.GetNames<TEnum>())}."];
        return null;
    }
}

/// <summary>The two work-item kinds listed by the flat Projects table.</summary>
internal enum ProjectItemKind
{
    Project,
    Activity,
}

internal sealed record ProjectItemListFilter(
    int? ProgramId,
    int? AxisId,
    ProjectStatus? Status,
    ProjectItemKind? Kind);
