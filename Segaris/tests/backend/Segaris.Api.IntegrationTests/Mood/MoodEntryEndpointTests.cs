using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Mood.Contracts;

namespace Segaris.Api.IntegrationTests.Mood;

public sealed class MoodEntryEndpointTests
{
    [Fact]
    public async Task Entries_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(
            MoodRequests.EntryRangePath(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 21)),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Options_return_fixed_vocabularies_for_authenticated_users()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var options = await client.GetFromJsonAsync<MoodOptionsResponse>(
            MoodRequests.OptionsPath,
            CancellationToken.None);

        Assert.NotNull(options);
        Assert.Equal(["Low", "Medium", "High"], options.Energies);
        Assert.Equal(["Negative", "Medium", "Positive"], options.Alignments);
        Assert.Equal(["Harmony", "Defensive", "Offensive", "Stability"], options.Directions);
        Assert.Equal(["Internal", "External"], options.Sources);
        Assert.Equal(72, options.Emotions.Count);
        Assert.Contains("Thoughtful", options.Emotions);
    }

    [Fact]
    public async Task Derived_emotion_preview_resolves_complete_criteria_without_csrf_or_persistence()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(
            MoodRequests.DerivedEmotionPreviewPath("High", "Positive", "Harmony", "Internal"),
            CancellationToken.None);
        var preview = await response.Content.ReadFromJsonAsync<MoodDerivedEmotionResponse>(
            CancellationToken.None);
        var log = await client.GetFromJsonAsync<MoodEntryListResponse>(
            MoodRequests.EntryRangePath(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 21)),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Happy", preview!.DerivedEmotion);
        Assert.Empty(log!.Entries);
    }

    [Fact]
    public async Task Derived_emotion_preview_requires_authentication_and_valid_criteria()
    {
        using var server = new CapexTestServer();
        using var anonymous = server.CreateClient();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var anonymousResponse = await anonymous.GetAsync(
            MoodRequests.DerivedEmotionPreviewPath("High", "Positive", "Harmony", "Internal"),
            CancellationToken.None);
        using var invalid = await client.GetAsync(
            MoodRequests.DerivedEmotionPreviewPath("Bogus", "Positive", "Harmony", "Internal"),
            CancellationToken.None);
        var problem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("mood.entry.validation", problem!.Code);
    }

    [Fact]
    public async Task Create_returns_owner_metadata_trimmed_notes_and_derived_emotion()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var request = MoodRequests.ValidEntry(
            new DateOnly(2026, 6, 18),
            score: 4,
            energy: MoodEnergy.Medium,
            alignment: MoodAlignment.Positive,
            notes: "  steady  ");

        using var response = await CapexApi.PostJsonAsync(client, MoodRequests.EntriesPath, request, csrf);
        var created = await response.Content.ReadFromJsonAsync<MoodEntryResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal(new DateOnly(2026, 6, 18), created.EntryDate);
        Assert.Equal(4, created.Score);
        Assert.Equal("Medium", created.Energy);
        Assert.Equal("Positive", created.Alignment);
        Assert.Equal("Harmony", created.Direction);
        Assert.Equal("Internal", created.Source);
        Assert.Equal("Optimistic", created.DerivedEmotion);
        Assert.Equal("steady", created.Notes);
        Assert.Equal(CapexTestServer.AdminUserName, created.CreatedByName);
        Assert.Null(created.UpdatedById);
    }

    [Fact]
    public async Task Create_requires_antiforgery_and_rejects_invalid_payloads()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var valid = MoodRequests.ValidEntry(new DateOnly(2026, 6, 18));

        using var withoutCsrf = await CapexApi.PostJsonAsync(client, MoodRequests.EntriesPath, valid, csrf: null);
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var badScore = await CapexApi.PostJsonAsync(
            client,
            MoodRequests.EntriesPath,
            valid with { Score = 6 },
            csrf);
        using var badCriteria = await CapexApi.PostJsonAsync(
            client,
            MoodRequests.EntriesPath,
            valid with { Energy = "Unknown" },
            csrf);
        using var badNotes = await CapexApi.PostJsonAsync(
            client,
            MoodRequests.EntriesPath,
            valid with { Notes = new string('a', 1001) },
            csrf);
        var scoreProblem = await badScore.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var criteriaProblem = await badCriteria.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var notesProblem = await badNotes.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badScore.StatusCode);
        Assert.Equal("mood.entry.validation", scoreProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, badCriteria.StatusCode);
        Assert.Equal("mood.entry.validation", criteriaProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, badNotes.StatusCode);
        Assert.Equal("mood.entry.validation", notesProblem!.Code);
    }

    [Fact]
    public async Task Detail_update_and_delete_are_owner_only_and_share_not_found_for_inaccessible_entries()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("mood-member", "MoodMemberPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "mood-member", "MoodMemberPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var create = MoodRequests.ValidEntry(new DateOnly(2026, 6, 18), notes: "member note");
        using var createdResponse = await CapexApi.PostJsonAsync(member, MoodRequests.EntriesPath, create, memberCsrf);
        var created = await createdResponse.Content.ReadFromJsonAsync<MoodEntryResponse>(CancellationToken.None);

        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        using var adminDetail = await admin.GetAsync(MoodRequests.EntryPath(created!.Id), CancellationToken.None);
        using var adminUpdate = await CapexApi.PutJsonAsync(
            admin,
            MoodRequests.EntryPath(created.Id),
            MoodRequests.ValidUpdate(new DateOnly(2026, 6, 19), notes: "admin edit"),
            adminCsrf);
        using var adminDelete = await CapexApi.DeleteAsync(admin, MoodRequests.EntryPath(created.Id), adminCsrf);
        using var memberUpdate = await CapexApi.PutJsonAsync(
            member,
            MoodRequests.EntryPath(created.Id),
            MoodRequests.ValidUpdate(
                new DateOnly(2026, 6, 19),
                score: 5,
                energy: MoodEnergy.High,
                alignment: MoodAlignment.Positive,
                direction: MoodDirection.Offensive,
                source: MoodSource.External,
                notes: "updated"),
            memberCsrf);
        var updated = await memberUpdate.Content.ReadFromJsonAsync<MoodEntryResponse>(CancellationToken.None);
        using var memberDelete = await CapexApi.DeleteAsync(member, MoodRequests.EntryPath(created.Id), memberCsrf);
        using var afterDelete = await member.GetAsync(MoodRequests.EntryPath(created.Id), CancellationToken.None);

        Assert.Equal(memberId, created.CreatedById);
        Assert.Equal(HttpStatusCode.NotFound, adminDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminDelete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, memberUpdate.StatusCode);
        Assert.Equal(new DateOnly(2026, 6, 19), updated!.EntryDate);
        Assert.Equal(5, updated.Score);
        Assert.Equal("Determined", updated.DerivedEmotion);
        Assert.Equal("updated", updated.Notes);
        Assert.Equal(memberId, updated.UpdatedById);
        Assert.Equal(HttpStatusCode.NoContent, memberDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Range_query_is_inclusive_orders_same_day_entries_and_returns_missing_day_averages()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var monday = new DateOnly(2026, 6, 15);

        var first = await CreateAsync(client, csrf, MoodRequests.ValidEntry(monday, score: 2, notes: "first"));
        var second = await CreateAsync(client, csrf, MoodRequests.ValidEntry(monday, score: 4, notes: "second"));
        var sunday = await CreateAsync(client, csrf, MoodRequests.ValidEntry(monday.AddDays(6), score: 5, notes: "sunday"));
        await CreateAsync(client, csrf, MoodRequests.ValidEntry(monday.AddDays(-1), score: 1, notes: "outside"));

        var log = await client.GetFromJsonAsync<MoodEntryListResponse>(
            MoodRequests.EntryRangePath(monday, monday.AddDays(6)),
            CancellationToken.None);

        Assert.NotNull(log);
        Assert.Equal(monday, log.From);
        Assert.Equal(monday.AddDays(6), log.To);
        Assert.Equal([first, second, sunday], log.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["first", "second", "sunday"], log.Entries.Select(entry => entry.Notes!).ToArray());
        Assert.Equal(7, log.DailyAverages.Count);
        Assert.Equal(3.0d, log.DailyAverages.Single(day => day.EntryDate == monday).AverageScore);
        Assert.Null(log.DailyAverages.Single(day => day.EntryDate == monday.AddDays(1)).AverageScore);
        Assert.Equal(5.0d, log.DailyAverages.Single(day => day.EntryDate == monday.AddDays(6)).AverageScore);
    }

    [Fact]
    public async Task Range_query_hides_other_users_entries_from_everyone_including_admins()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("mood-private", "MoodPrivatePass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "mood-private", "MoodPrivatePass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        await CreateAsync(
            member,
            memberCsrf,
            MoodRequests.ValidEntry(new DateOnly(2026, 6, 18), score: 2, notes: "private"));

        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        await CreateAsync(
            admin,
            adminCsrf,
            MoodRequests.ValidEntry(new DateOnly(2026, 6, 18), score: 5, notes: "admin"));

        var memberLog = await member.GetFromJsonAsync<MoodEntryListResponse>(
            MoodRequests.EntryRangePath(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 21)),
            CancellationToken.None);
        var adminLog = await admin.GetFromJsonAsync<MoodEntryListResponse>(
            MoodRequests.EntryRangePath(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 21)),
            CancellationToken.None);

        Assert.NotNull(memberLog);
        Assert.Single(memberLog.Entries);
        Assert.Equal(memberId, memberLog.Entries[0].CreatedById);
        Assert.Equal("private", memberLog.Entries[0].Notes);
        Assert.NotNull(adminLog);
        Assert.Single(adminLog.Entries);
        Assert.Equal("admin", adminLog.Entries[0].Notes);
        Assert.Equal(5.0d, adminLog.DailyAverages.Single(day => day.EntryDate == new DateOnly(2026, 6, 18)).AverageScore);
    }

    [Fact]
    public async Task Range_query_rejects_missing_or_reversed_bounds()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var missing = await client.GetAsync(MoodRequests.EntriesPath, CancellationToken.None);
        using var reversed = await client.GetAsync(
            MoodRequests.EntryRangePath(new DateOnly(2026, 6, 21), new DateOnly(2026, 6, 15)),
            CancellationToken.None);
        var missingProblem = await missing.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var reversedProblem = await reversed.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("mood.range.validation", missingProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal("mood.range.validation", reversedProblem!.Code);
    }

    private static async Task<int> CreateAsync(
        HttpClient client,
        string csrf,
        CreateMoodEntryRequest request)
    {
        using var response = await CapexApi.PostJsonAsync(client, MoodRequests.EntriesPath, request, csrf);
        response.EnsureSuccessStatusCode();
        var entry = await response.Content.ReadFromJsonAsync<MoodEntryResponse>(CancellationToken.None);
        Assert.NotNull(entry);
        return entry.Id;
    }

    private sealed record ProblemPayload(string? Code);
}
