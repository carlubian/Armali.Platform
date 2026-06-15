using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Opex.Queries;

/// <summary>
/// Bound query surface for <c>GET /api/opex/contracts</c>. Property names map to
/// the frozen query-parameter vocabulary in <see cref="OpexContractQuery"/>; the
/// conversion methods validate the inputs and project them onto the platform
/// pagination/sort primitives and the normalized <see cref="OpexContractFilter"/>.
/// </summary>
internal sealed class OpexContractListQuery
{
    public string? Search { get; init; }

    public string? Type { get; init; }

    public string? Status { get; init; }

    public int? Category { get; init; }

    public int? Supplier { get; init; }

    public int? CostCenter { get; init; }

    public int? Currency { get; init; }

    public string? Frequency { get; init; }

    public string? Visibility { get; init; }

    public int? Creator { get; init; }

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
            // The documented default ordering is contract name ascending, which is
            // the platform's default direction; an explicit field keeps it too.
            return SortRequest.Create(
                Sort,
                SortDirection,
                OpexContractQuery.AllowedSortFields,
                OpexContractQuery.SortFields.Default,
                OpexContractQuery.SortFields.TieBreaker);
        }
        catch (ArgumentException exception)
        {
            var field = exception.ParamName == "direction"
                ? OpexContractQuery.Parameters.SortDirection
                : OpexContractQuery.Parameters.Sort;
            throw Problem(field, exception.Message);
        }
    }

    public OpexContractFilter ToFilter()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var movementType = ParseEnum<OpexMovementType>(Type, OpexContractQuery.Parameters.Type, errors);
        var status = ParseEnum<OpexContractStatus>(Status, OpexContractQuery.Parameters.Status, errors);
        var frequency = ParseEnum<OpexExpectedFrequency>(Frequency, OpexContractQuery.Parameters.Frequency, errors);
        var visibility = ParseEnum<RecordVisibility>(Visibility, OpexContractQuery.Parameters.Visibility, errors);
        Throw(errors);

        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        return new OpexContractFilter(
            search,
            movementType,
            status,
            Category,
            Supplier,
            CostCenter,
            Currency,
            frequency,
            visibility,
            Creator);
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
