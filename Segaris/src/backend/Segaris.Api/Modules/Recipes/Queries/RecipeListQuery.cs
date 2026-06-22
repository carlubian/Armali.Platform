using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Recipes.Queries;

/// <summary>
/// Bound query surface for <c>GET /api/recipes</c>. Property names map to the
/// frozen recipe gallery query vocabulary.
/// </summary>
internal sealed class RecipeListQuery
{
    public string? Search { get; init; }

    public int? Category { get; init; }

    public string? Difficulty { get; init; }

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
                RecipeQuery.AllowedSortFields,
                RecipeQuery.SortFields.Default,
                RecipeQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction"
                ? "sortDirection"
                : "sort";
            throw Problem(field, exception.Message);
        }
    }

    public RecipeFilter ToFilter()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var difficulty = ParseEnum<RecipeDifficulty>(Difficulty, "difficulty", errors);
        Throw(errors);

        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new RecipeFilter(search, Category, difficulty);
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

internal sealed record RecipeFilter(string? Search, int? CategoryId, RecipeDifficulty? Difficulty);
