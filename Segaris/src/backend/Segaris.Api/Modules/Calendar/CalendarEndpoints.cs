using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Domain;
using Segaris.Api.Modules.Calendar.Mutations;
using Segaris.Api.Modules.Calendar.Queries;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar;

internal static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(CalendarApiRoutes.Calendar, CalendarApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet(CalendarApiRoutes.Notes, ListNotesAsync)
            .WithName("ListCalendarNotes")
            .WithSummary("Returns accessible Calendar daily notes for an inclusive date range")
            .Produces<IReadOnlyList<CalendarDailyNoteResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost(CalendarApiRoutes.Notes, CreateNoteAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateCalendarNote")
            .WithSummary("Creates a Calendar daily note")
            .Produces<CalendarDailyNoteResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet(CalendarApiRoutes.NoteById, GetNoteAsync)
            .WithName("GetCalendarNote")
            .WithSummary("Returns one accessible Calendar daily note")
            .Produces<CalendarDailyNoteResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(CalendarApiRoutes.NoteById, UpdateNoteAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateCalendarNote")
            .WithSummary("Replaces one accessible Calendar daily note")
            .Produces<CalendarDailyNoteResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(CalendarApiRoutes.NoteById, DeleteNoteAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteCalendarNote")
            .WithSummary("Deletes one accessible Calendar daily note")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> ListNotesAsync(
        DateOnly? from,
        DateOnly? to,
        CalendarDailyNoteReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var (rangeFrom, rangeTo) = ValidateRange(from, to);
        var notes = await read.ListNotesAsync(rangeFrom, rangeTo, userId, cancellationToken);
        return TypedResults.Ok(notes);
    }

    private static async Task<IResult> GetNoteAsync(
        int noteId,
        CalendarDailyNoteReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var note = await read.GetNoteAsync(noteId, userId, cancellationToken);
        if (note is null)
        {
            throw CalendarProblem.NoteNotFound();
        }

        return TypedResults.Ok(note);
    }

    private static async Task<IResult> CreateNoteAsync(
        UpsertCalendarDailyNoteRequest request,
        CalendarDailyNoteWriteService write,
        CalendarDailyNoteReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int noteId;
        try
        {
            noteId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (CalendarValidationException exception)
        {
            throw CalendarProblem.From(exception);
        }

        var created = await read.GetNoteAsync(noteId, userId, cancellationToken);
        return TypedResults.Created($"/api/calendar/notes/{noteId}", created);
    }

    private static async Task<IResult> UpdateNoteAsync(
        int noteId,
        UpsertCalendarDailyNoteRequest request,
        CalendarDailyNoteWriteService write,
        CalendarDailyNoteReadService read,
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
            updated = await write.UpdateAsync(noteId, request, userId, cancellationToken);
        }
        catch (CalendarValidationException exception)
        {
            throw CalendarProblem.From(exception);
        }

        if (!updated)
        {
            throw CalendarProblem.NoteNotFound();
        }

        var note = await read.GetNoteAsync(noteId, userId, cancellationToken);
        return TypedResults.Ok(note);
    }

    private static async Task<IResult> DeleteNoteAsync(
        int noteId,
        CalendarDailyNoteWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(noteId, userId, cancellationToken);
        if (!deleted)
        {
            throw CalendarProblem.NoteNotFound();
        }

        return TypedResults.NoContent();
    }

    private static (DateOnly From, DateOnly To) ValidateRange(DateOnly? from, DateOnly? to)
    {
        if (from is null)
        {
            throw CalendarProblem.RangeInvalid(CalendarApiRoutes.QueryParameters.From, "The from date is required.");
        }

        if (to is null)
        {
            throw CalendarProblem.RangeInvalid(CalendarApiRoutes.QueryParameters.To, "The to date is required.");
        }

        if (to < from)
        {
            throw CalendarProblem.RangeInvalid(
                CalendarApiRoutes.QueryParameters.To,
                "The to date must be on or after the from date.");
        }

        return (from.Value, to.Value);
    }
}
