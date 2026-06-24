using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Calendar.Contracts;

namespace Segaris.Api.IntegrationTests.Calendar;

public sealed class CalendarNoteEndpointTests
{
    private const string NotesPath = "/api/calendar/notes";

    [Fact]
    public async Task Notes_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_defaults_private_and_trims_text()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            NotesPath,
            ValidNote(title: "  Errands  ", body: "  Pick package  ", visibility: null),
            csrf);
        var created = await response.Content.ReadFromJsonAsync<CalendarDailyNoteResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(new DateOnly(2026, 6, 24), created.Date);
        Assert.Equal("Errands", created.Title);
        Assert.Equal("Pick package", created.Body);
        Assert.Equal("Private", created.Visibility);
        Assert.Equal(CapexTestServer.AdminUserName, created.CreatedByName);
        Assert.Equal(created.CreatedById, created.UpdatedById);
    }

    [Fact]
    public async Task Create_requires_antiforgery_and_rejects_invalid_payloads()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var valid = ValidNote();

        using var withoutCsrf = await CapexApi.PostJsonAsync(client, NotesPath, valid, csrf: null);
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var blankBody = await CapexApi.PostJsonAsync(client, NotesPath, valid with { Body = "   " }, csrf);
        using var missingDate = await CapexApi.PostJsonAsync(client, NotesPath, valid with { Date = null }, csrf);
        using var longTitle = await CapexApi.PostJsonAsync(
            client,
            NotesPath,
            valid with { Title = new string('a', 201) },
            csrf);
        using var badVisibility = await CapexApi.PostJsonAsync(client, NotesPath, valid with { Visibility = "Shared" }, csrf);
        var blankProblem = await blankBody.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var dateProblem = await missingDate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var titleProblem = await longTitle.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var visibilityProblem = await badVisibility.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blankBody.StatusCode);
        Assert.Equal("calendar.note.validation", blankProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, missingDate.StatusCode);
        Assert.Equal("calendar.note.validation", dateProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, longTitle.StatusCode);
        Assert.Equal("calendar.note.validation", titleProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, badVisibility.StatusCode);
        Assert.Equal("calendar.note.validation", visibilityProblem!.Code);
    }

    [Fact]
    public async Task Detail_update_delete_and_range_filter_work_for_accessible_notes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var first = await CreateAsync(client, csrf, ValidNote(date: new DateOnly(2026, 6, 24), body: "first"));
        var second = await CreateAsync(client, csrf, ValidNote(date: new DateOnly(2026, 6, 24), body: "second"));
        await CreateAsync(client, csrf, ValidNote(date: new DateOnly(2026, 7, 1), body: "outside"));

        var detail = await client.GetFromJsonAsync<CalendarDailyNoteResponse>(NotePath(first), CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            NotePath(first),
            ValidNote(date: new DateOnly(2026, 6, 25), title: "Updated", body: "changed", visibility: "Public"),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<CalendarDailyNoteResponse>(CancellationToken.None);
        var range = await client.GetFromJsonAsync<IReadOnlyList<CalendarDailyNoteResponse>>(
            RangePath(new DateOnly(2026, 6, 24), new DateOnly(2026, 6, 25)),
            CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(client, NotePath(first), csrf);
        using var afterDelete = await client.GetAsync(NotePath(first), CancellationToken.None);

        Assert.NotNull(detail);
        Assert.Equal("first", detail.Body);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(new DateOnly(2026, 6, 25), updated!.Date);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal("changed", updated.Body);
        Assert.Equal("Public", updated.Visibility);
        Assert.NotNull(range);
        Assert.Equal([second, first], range.Select(note => note.Id).ToArray());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Public_collaboration_private_isolation_and_creator_only_visibility_change_are_enforced()
    {
        using var server = new CapexTestServer();
        using var owner = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        var publicNote = await CreateAsync(owner, ownerCsrf, ValidNote(body: "shared", visibility: "Public"));
        var privateNote = await CreateAsync(owner, ownerCsrf, ValidNote(body: "secret", visibility: "Private"));

        await server.CreateUserAsync("calendar-member", "CalendarMember123!");
        using var member = await server.CreateAuthenticatedClientAsync("calendar-member", "CalendarMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            NotePath(publicNote),
            ValidNote(body: "member edit", visibility: "Public"),
            memberCsrf);
        var publicEdited = await editPublic.Content.ReadFromJsonAsync<CalendarDailyNoteResponse>(CancellationToken.None);
        using var hidePublic = await CapexApi.PutJsonAsync(
            member,
            NotePath(publicNote),
            ValidNote(body: "hide", visibility: "Private"),
            memberCsrf);
        using var getPrivate = await member.GetAsync(NotePath(privateNote), CancellationToken.None);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            NotePath(privateNote),
            ValidNote(body: "leak", visibility: "Private"),
            memberCsrf);
        var memberRange = await member.GetFromJsonAsync<IReadOnlyList<CalendarDailyNoteResponse>>(
            RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal("member edit", publicEdited!.Body);
        Assert.Equal("Public", publicEdited.Visibility);
        Assert.Equal(HttpStatusCode.Forbidden, hidePublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.NotNull(memberRange);
        Assert.Equal([publicNote], memberRange.Select(note => note.Id).ToArray());
    }

    [Fact]
    public async Task Range_query_rejects_missing_or_reversed_bounds()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var missing = await client.GetAsync(NotesPath, CancellationToken.None);
        using var reversed = await client.GetAsync(
            RangePath(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1)),
            CancellationToken.None);
        var missingProblem = await missing.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var reversedProblem = await reversed.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("calendar.note.validation", missingProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal("calendar.note.validation", reversedProblem!.Code);
    }

    private static async Task<int> CreateAsync(
        HttpClient client,
        string csrf,
        UpsertCalendarDailyNoteRequest request)
    {
        using var response = await CapexApi.PostJsonAsync(client, NotesPath, request, csrf);
        response.EnsureSuccessStatusCode();
        var note = await response.Content.ReadFromJsonAsync<CalendarDailyNoteResponse>(CancellationToken.None);
        Assert.NotNull(note);
        return note.Id;
    }

    private static UpsertCalendarDailyNoteRequest ValidNote(
        DateOnly? date = null,
        string? title = null,
        string body = "Body",
        string? visibility = "Private") =>
        new(date ?? new DateOnly(2026, 6, 24), title, body, visibility!);

    private static string NotePath(int noteId) => $"{NotesPath}/{noteId}";

    private static string RangePath(DateOnly from, DateOnly to) => $"{NotesPath}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

    private sealed record ProblemPayload(string? Code);
}
