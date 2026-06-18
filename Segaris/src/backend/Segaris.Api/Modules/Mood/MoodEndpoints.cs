using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Api.Modules.Mood.Mutations;
using Segaris.Api.Modules.Mood.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

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

        group.MapGet("/derived-emotion", GetDerivedEmotion)
            .WithName("GetMoodDerivedEmotion")
            .WithSummary("Previews the derived emotion for a complete criteria combination")
            .Produces<MoodDerivedEmotionResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetMoodDashboard")
            .WithSummary("Returns owner-only strict-period dashboard aggregates for the current user")
            .Produces<MoodDashboardResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

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

    private static IResult GetDerivedEmotion(
        string? energy,
        string? alignment,
        string? direction,
        string? source)
    {
        try
        {
            var derivedEmotion = MoodDerivedEmotionMatrix.Resolve(
                ParseCriterion<MoodEnergy>(energy, "energy"),
                ParseCriterion<MoodAlignment>(alignment, "alignment"),
                ParseCriterion<MoodDirection>(direction, "direction"),
                ParseCriterion<MoodSource>(source, "source"));
            return TypedResults.Ok(new MoodDerivedEmotionResponse(derivedEmotion));
        }
        catch (MoodValidationException exception)
        {
            throw MoodProblem.From(exception);
        }
    }

    private static async Task<IResult> GetDashboardAsync(
        string? scale,
        string? period,
        MoodDashboardService dashboard,
        ICurrentUser currentUser,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var resolved = ResolvePeriod(scale, period, clock);
        var result = await dashboard.GetDashboardAsync(resolved, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

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

    private static MoodPeriod ResolvePeriod(string? scale, string? period, IClock clock)
    {
        var dashboardScale = MoodDashboardScale.Year;
        if (!string.IsNullOrWhiteSpace(scale) && !MoodPeriod.TryParseScale(scale, out dashboardScale))
        {
            throw MoodProblem.PeriodInvalid(MoodApiRoutes.ScaleQuery, "The dashboard scale is unknown.");
        }

        if (string.IsNullOrWhiteSpace(period))
        {
            var today = MoodDefaults.Today(clock.UtcNow);
            return MoodPeriod.Current(dashboardScale, today);
        }

        if (!MoodPeriod.TryParse(dashboardScale, period, out var resolved))
        {
            throw MoodProblem.PeriodInvalid(
                MoodApiRoutes.PeriodQuery,
                "The dashboard period token is malformed for the selected scale.");
        }

        return resolved;
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

    private static TEnum ParseCriterion<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new MoodValidationException(
            $"The {field} criterion is not a recognized value.",
            MoodValidationReason.Criteria);
    }
}
