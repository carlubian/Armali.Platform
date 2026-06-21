using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Processes.Queries;

/// <summary>Bound query surface for <c>GET /api/processes</c>.</summary>
internal sealed class ProcessListQuery
{
    public string? Search { get; init; }

    public int? Category { get; init; }

    public string? Status { get; init; }

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
                ProcessesQuery.AllowedSortFields,
                ProcessesQuery.SortFields.Default,
                ProcessesQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction" ? "sortDirection" : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public ProcessFilter ToFilter()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var visibility = ParseEnum<RecordVisibility>(Visibility, "visibility", errors);
        var status = ParseStatus(Status, errors);
        Throw(errors);

        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new ProcessFilter(search, Category, status, Creator, visibility);
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

    private static string? ParseStatus(string? value, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (ProcessExecution.StatusNames.Contains(value, StringComparer.Ordinal))
        {
            return value;
        }

        errors["status"] =
            [$"'{value}' is not a valid status. Allowed values: {string.Join(", ", ProcessExecution.StatusNames)}."];
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

internal sealed record ProcessFilter(
    string? Search,
    int? CategoryId,
    string? Status,
    int? CreatorId,
    RecordVisibility? Visibility);
