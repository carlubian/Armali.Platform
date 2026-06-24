using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Calendar;
using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Projection;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Calendar;

public sealed class CalendarEntriesEndpointTests
{
    private const string EntriesPath = "/api/calendar/entries";
    private const string NotesPath = "/api/calendar/notes";

    [Fact]
    public async Task Entries_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Entries_reject_invalid_ranges_and_filters_with_stable_codes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var missing = await client.GetAsync(EntriesPath, CancellationToken.None);
        using var malformed = await client.GetAsync($"{EntriesPath}?from=2026-06&to=2026-06-30", CancellationToken.None);
        using var reversed = await client.GetAsync(
            RangePath(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1)),
            CancellationToken.None);
        using var oversized = await client.GetAsync(
            RangePath(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 2)),
            CancellationToken.None);
        using var badSource = await client.GetAsync($"{RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30))}&sourceModule=capex", CancellationToken.None);
        using var badFamily = await client.GetAsync($"{RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30))}&visualFamily=Finance", CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("calendar.entries.range_invalid", (await ReadProblemAsync(missing)).Code);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal("calendar.entries.range_invalid", (await ReadProblemAsync(malformed)).Code);
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal("calendar.entries.range_invalid", (await ReadProblemAsync(reversed)).Code);
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        Assert.Equal("calendar.entries.range_invalid", (await ReadProblemAsync(oversized)).Code);
        Assert.Equal(HttpStatusCode.BadRequest, badSource.StatusCode);
        Assert.Equal("calendar.entries.source_module_unsupported", (await ReadProblemAsync(badSource)).Code);
        Assert.Equal(HttpStatusCode.BadRequest, badFamily.StatusCode);
        Assert.Equal("calendar.entries.visual_family_unsupported", (await ReadProblemAsync(badFamily)).Code);
    }

    [Fact]
    public async Task Entries_mix_notes_and_registered_projection_providers_in_deterministic_order()
    {
        var fakeEntries = new[]
        {
            Projection(
                "travel:trip:9",
                CalendarSourceModules.Travel,
                CalendarSourceTypes.Trip,
                CalendarVisualFamilies.Travel,
                "Barcelona",
                new DateOnly(2026, 6, 24),
                endDate: new DateOnly(2026, 6, 26)),
            Projection(
                "inventory:order:3",
                CalendarSourceModules.Inventory,
                CalendarSourceTypes.InventoryOrderExpectedReceipt,
                CalendarVisualFamilies.Other,
                "Desk delivery",
                new DateOnly(2026, 6, 23)),
        };
        using var server = new CapexTestServer(configureServices: services =>
        {
            services.AddSingleton<ICalendarProjectionProvider>(new FakeCalendarProjectionProvider(CalendarSourceModules.Travel, [fakeEntries[0]]));
            services.AddSingleton<ICalendarProjectionProvider>(new FakeCalendarProjectionProvider(CalendarSourceModules.Inventory, [fakeEntries[1]]));
        });
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var noteId = await CreateNoteAsync(client, csrf, new DateOnly(2026, 6, 24), "House note", "Water plants", "Private");

        var entries = await client.GetFromJsonAsync<IReadOnlyList<CalendarEntryResponse>>(
            RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        Assert.NotNull(entries);
        Assert.Equal(["inventory:order:3", $"calendar:note:{noteId}", "travel:trip:9"], entries.Select(entry => entry.Id).ToArray());
        var note = Assert.Single(entries, entry => entry.Id == $"calendar:note:{noteId}");
        Assert.Equal(CalendarSourceModules.Calendar, note.SourceModule);
        Assert.Equal(CalendarSourceTypes.DailyNote, note.SourceType);
        Assert.Equal(CalendarVisualFamilies.Note, note.VisualFamily);
        Assert.Equal("House note", note.Title);
        Assert.Equal("Water plants", note.Subtitle);
        Assert.Equal("Private", note.Status);
        Assert.Equal($"/calendar?day=2026-06-24&noteId={noteId}", note.TargetRoute);
    }

    [Fact]
    public async Task Entries_apply_source_module_and_visual_family_filters()
    {
        var fakeEntries = new[]
        {
            Projection(
                "travel:trip:9",
                CalendarSourceModules.Travel,
                CalendarSourceTypes.Trip,
                CalendarVisualFamilies.Travel,
                "Barcelona",
                new DateOnly(2026, 6, 24)),
        };
        using var server = new CapexTestServer(configureServices: services =>
            services.AddSingleton<ICalendarProjectionProvider>(new FakeCalendarProjectionProvider(CalendarSourceModules.Travel, fakeEntries)));
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var noteId = await CreateNoteAsync(client, csrf, new DateOnly(2026, 6, 24), "House note", "Water plants", "Private");

        var calendarOnly = await client.GetFromJsonAsync<IReadOnlyList<CalendarEntryResponse>>(
            $"{RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30))}&sourceModule=calendar",
            CancellationToken.None);
        var travelOnly = await client.GetFromJsonAsync<IReadOnlyList<CalendarEntryResponse>>(
            $"{RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30))}&visualFamily=Travel",
            CancellationToken.None);

        Assert.NotNull(calendarOnly);
        Assert.Equal([$"calendar:note:{noteId}"], calendarOnly.Select(entry => entry.Id).ToArray());
        Assert.NotNull(travelOnly);
        Assert.Equal(["travel:trip:9"], travelOnly.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public async Task Entries_exclude_inaccessible_private_notes_from_mixed_response()
    {
        using var server = new CapexTestServer();
        using var owner = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        var privateNote = await CreateNoteAsync(owner, ownerCsrf, new DateOnly(2026, 6, 24), "Secret", "Hidden", "Private");
        var publicNote = await CreateNoteAsync(owner, ownerCsrf, new DateOnly(2026, 6, 24), "Shared", "Visible", "Public");

        await server.CreateUserAsync("calendar-reader", "CalendarReader123!");
        using var reader = await server.CreateAuthenticatedClientAsync("calendar-reader", "CalendarReader123!");

        var entries = await reader.GetFromJsonAsync<IReadOnlyList<CalendarEntryResponse>>(
            RangePath(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        Assert.NotNull(entries);
        Assert.DoesNotContain(entries, entry => entry.Id == $"calendar:note:{privateNote}");
        Assert.Contains(entries, entry => entry.Id == $"calendar:note:{publicNote}");
    }

    private static async Task<int> CreateNoteAsync(
        HttpClient client,
        string csrf,
        DateOnly date,
        string title,
        string body,
        string visibility)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            NotesPath,
            new UpsertCalendarDailyNoteRequest(date, title, body, visibility),
            csrf);
        response.EnsureSuccessStatusCode();
        var note = await response.Content.ReadFromJsonAsync<CalendarDailyNoteResponse>(CancellationToken.None);
        Assert.NotNull(note);
        return note.Id;
    }

    private static string RangePath(DateOnly from, DateOnly to) =>
        $"{EntriesPath}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

    private static async Task<ProblemPayload> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.NotNull(problem);
        return problem;
    }

    private static NormalizedCalendarProjection Projection(
        string id,
        string sourceModule,
        string sourceType,
        string visualFamily,
        string title,
        DateOnly startDate,
        DateOnly? endDate = null) => new(
            id,
            sourceModule,
            sourceType,
            visualFamily,
            title,
            null,
            startDate,
            endDate,
            true,
            null,
            null);

    private sealed record ProblemPayload(string? Code);

    private sealed class FakeCalendarProjectionProvider(
        string sourceModule,
        IReadOnlyList<NormalizedCalendarProjection> entries) : ICalendarProjectionProvider
    {
        public string SourceModule { get; } = sourceModule;

        public Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
            DateOnly from,
            DateOnly to,
            UserId viewer,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NormalizedCalendarProjection>>(
                entries
                    .Where(entry => entry.StartDate <= to && (entry.EndDate ?? entry.StartDate) >= from)
                    .ToArray());
    }
}
