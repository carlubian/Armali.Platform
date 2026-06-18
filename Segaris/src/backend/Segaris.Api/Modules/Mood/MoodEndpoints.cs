using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Api.Modules.Mood.Mutations;
using Segaris.Api.Modules.Mood.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Mood;

/// <summary>
/// HTTP surface for the Mood module. Entry APIs are owner-only current-user data
/// and state-changing routes require antiforgery.
/// </summary>
internal static class MoodEndpoints
{
    public static IEndpointRouteBuilder MapMoodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("mood", MoodApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/options", GetOptions)
            .WithName("GetMoodOptions")
            .WithSummary("Returns fixed Mood criteria and derived-emotion codes")
            .Produces<MoodOptionsResponse>();

        var entries = group.MapGroup("/entries");

        entries.MapGet("", ListEntriesAsync)
            .WithName("ListMoodEntries")
            .WithSummary("Returns owner-only Mood entries and daily averages for an inclusive date range")
            .Produces<MoodEntryListResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        entries.MapPost("", CreateEntryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateMoodEntry")
            .WithSummary("Creates a current-user Mood entry")
            .Produces<MoodEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        entries.MapGet(MoodApiRoutes.EntryById, GetEntryAsync)
            .WithName("GetMoodEntry")
            .WithSummary("Returns one current-user Mood entry")
            .Produces<MoodEntryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        entries.MapPut(MoodApiRoutes.EntryById, UpdateEntryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateMoodEntry")
            .WithSummary("Updates one current-user Mood entry")
            .Produces<MoodEntryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        entries.MapDelete(MoodApiRoutes.EntryById, DeleteEntryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteMoodEntry")
            .WithSummary("Deletes one current-user Mood entry")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static IResult GetOptions(MoodReadService read) => TypedResults.Ok(read.GetOptions());

    private static async Task<IResult> ListEntriesAsync(
        DateOnly? from,
        DateOnly? to,
        MoodReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var (rangeFrom, rangeTo) = ValidateRange(from, to);
        var result = await read.ListEntriesAsync(rangeFrom, rangeTo, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetEntryAsync(
        int entryId,
        MoodReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var entry = await read.GetEntryAsync(entryId, userId, cancellationToken);
        if (entry is null)
        {
            throw MoodProblem.EntryNotFound();
        }

        return TypedResults.Ok(entry);
    }

    private static async Task<IResult> CreateEntryAsync(
        CreateMoodEntryRequest request,
        MoodEntryWriteService write,
        MoodReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int entryId;
        try
        {
            entryId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (MoodValidationException exception)
        {
            throw MoodProblem.From(exception);
        }

        var created = await read.GetEntryAsync(entryId, userId, cancellationToken);
        return TypedResults.Created($"/api/mood/entries/{entryId}", created);
    }

    private static async Task<IResult> UpdateEntryAsync(
        int entryId,
        UpdateMoodEntryRequest request,
        MoodEntryWriteService write,
        MoodReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateAsync(entryId, request, userId, cancellationToken);
        }
        catch (MoodValidationException exception)
        {
            throw MoodProblem.From(exception);
        }

        if (!updated)
        {
            throw MoodProblem.EntryNotFound();
        }

        var entry = await read.GetEntryAsync(entryId, userId, cancellationToken);
        return TypedResults.Ok(entry);
    }

    private static async Task<IResult> DeleteEntryAsync(
        int entryId,
        MoodEntryWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(entryId, userId, cancellationToken);
        if (!deleted)
        {
            throw MoodProblem.EntryNotFound();
        }

        return TypedResults.NoContent();
    }

    private static (DateOnly From, DateOnly To) ValidateRange(DateOnly? from, DateOnly? to)
    {
        if (from is null)
        {
            throw MoodProblem.RangeInvalid(MoodApiRoutes.FromQuery, "The from date is required.");
        }

        if (to is null)
        {
            throw MoodProblem.RangeInvalid(MoodApiRoutes.ToQuery, "The to date is required.");
        }

        if (to < from)
        {
            throw MoodProblem.RangeInvalid(MoodApiRoutes.ToQuery, "The to date must be on or after the from date.");
        }

        return (from.Value, to.Value);
    }
}
