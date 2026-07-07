using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Games.Queries;

/// <summary>
/// Query-string binding for the paginated playthrough collection. Filter values are
/// exact except for the partial <see cref="Search"/>, which matches playthrough name
/// and game name. Parameter names mirror the frozen frontend query contract.
/// </summary>
internal sealed class PlaythroughListQuery
{
    public string? Search { get; init; }

    public int? Game { get; init; }

    public string? Platform { get; init; }

    public string? Status { get; init; }

    public string? Tag { get; init; }

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

        if (!PlaythroughQuery.PageSizeOptions.Contains(pageSize))
        {
            errors["pageSize"] =
            [$"Page size must be one of: {string.Join(", ", PlaythroughQuery.PageSizeOptions)}."];
        }

        if (errors.Count > 0)
        {
            throw Problem(errors);
        }

        return new PaginationRequest(page, pageSize);
    }

    public SortRequest ToSort()
    {
        try
        {
            return SortRequest.Create(
                Sort,
                SortDirection ?? PlaythroughQuery.DefaultSortDirection,
                PlaythroughQuery.AllowedSortFields,
                PlaythroughQuery.SortFields.Default,
                PlaythroughQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction" ? "sortDirection" : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public PlaythroughFilter ToFilter()
    {
        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        var tag = string.IsNullOrWhiteSpace(Tag) ? null : Tag.Trim().ToUpperInvariant();
        return new PlaythroughFilter(
            search,
            Game,
            ParsePlatform(),
            ParseStatus(),
            tag,
            Creator,
            ParseVisibility());
    }

    private GamePlatform? ParsePlatform()
    {
        if (string.IsNullOrWhiteSpace(Platform))
        {
            return null;
        }

        if (Enum.TryParse<GamePlatform>(Platform.Trim(), ignoreCase: false, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw Problem("platform", "Platform is not a recognized value.");
    }

    private PlaythroughStatus? ParseStatus()
    {
        if (string.IsNullOrWhiteSpace(Status))
        {
            return null;
        }

        if (Enum.TryParse<PlaythroughStatus>(Status.Trim(), ignoreCase: false, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw Problem("status", "Status is not a recognized value.");
    }

    private RecordVisibility? ParseVisibility()
    {
        if (string.IsNullOrWhiteSpace(Visibility))
        {
            return null;
        }

        if (Enum.TryParse<RecordVisibility>(Visibility.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw Problem("visibility", "Visibility must be Public or Private.");
    }

    private static ApiProblemException Problem(Dictionary<string, string[]> errors) => new(
        StatusCodes.Status400BadRequest,
        ApiErrorCodes.BadRequest,
        "One or more request values are invalid.",
        errors: errors);

    private static ApiProblemException Problem(string field, string message) => Problem(
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [field] = [message],
        });
}

internal sealed record PlaythroughFilter(
    string? Search,
    int? GameId,
    GamePlatform? Platform,
    PlaythroughStatus? Status,
    string? NormalizedTag,
    int? CreatorId,
    RecordVisibility? Visibility);
