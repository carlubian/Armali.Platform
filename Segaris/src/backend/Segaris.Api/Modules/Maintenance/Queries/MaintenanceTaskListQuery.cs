using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Maintenance.Queries;

/// <summary>Bound query surface for <c>GET /api/maintenance/tasks</c>.</summary>
internal sealed class MaintenanceTaskListQuery
{
    public string? Search { get; init; }

    public int? Type { get; init; }

    public string? Status { get; init; }

    public string? Priority { get; init; }

    public int? Asset { get; init; }

    public int? Creator { get; init; }

    public string? Visibility { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? Sort { get; init; }

    public string? SortDirection { get; init; }

    public PaginationRequest ToPagination()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var page = Page ?? PaginationRequest.DefaultPage;
        var pageSize = PageSize ?? PaginationRequest.DefaultPageSize;

        if (page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (pageSize < 1 || pageSize > PaginationRequest.MaximumPageSize)
        {
            errors["pageSize"] =
                [$"Page size must be between 1 and {PaginationRequest.MaximumPageSize}."];
        }

        Throw(errors);
        return new PaginationRequest(page, pageSize);
    }

    public SortRequest ToSort()
    {
        try
        {
            return SortRequest.Create(
                Sort,
                SortDirection,
                MaintenanceQuery.AllowedSortFields,
                MaintenanceQuery.SortFields.Default,
                MaintenanceQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction" ? "sortDirection" : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public MaintenanceTaskFilter ToFilter()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var status = ParseEnum<MaintenanceStatus>(Status, "status", errors);
        var priority = ParseEnum<MaintenancePriority>(Priority, "priority", errors);
        var visibility = ParseEnum<RecordVisibility>(Visibility, "visibility", errors);
        Throw(errors);

        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new MaintenanceTaskFilter(search, Type, status, priority, Asset, Creator, visibility);
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

    private static void Throw(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }
    }

    private static ApiProblemException Problem(string field, string message) => new(
        StatusCodes.Status400BadRequest,
        ApiErrorCodes.BadRequest,
        "One or more request values are invalid.",
        errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}

internal sealed record MaintenanceTaskFilter(
    string? Search,
    int? TypeId,
    MaintenanceStatus? Status,
    MaintenancePriority? Priority,
    int? AssetId,
    int? CreatorId,
    RecordVisibility? Visibility);
