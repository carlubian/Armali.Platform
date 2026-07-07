using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Games;

public sealed class PlaythroughEndpointTests
{
    private const string MemberName = "member";
    private const string MemberPassword = "MemberPass123!";

    [Fact]
    public async Task Playthroughs_require_authentication()
    {
        using var server = new CapexTestServer();
        using var anonymous = server.CreateClient();

        using var response = await anonymous.GetAsync("/api/games/playthroughs", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_applies_creation_defaults_and_normalizes_tags()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var game = await CreateGameAsync(client, csrf, "Baldur's Gate 3", "PC");

        var created = await CreatePlaythroughAsync(
            client,
            csrf,
            new CreatePlaythroughRequest(
                "Honour run",
                game.Id,
                2026,
                7,
                Status: null,
                Tags: ["Solo", "solo", "  ", "Ironman"],
                Visibility: null));

        Assert.Equal("Planning", created.Status);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal(game.Id, created.GameId);
        Assert.Equal("Baldur's Gate 3", created.GameName);
        Assert.Equal("PC", created.Platform);
        Assert.Equal((2026, 7), (created.StartYear, created.StartMonth));
        Assert.Equal(["Solo", "Ironman"], created.Tags);
        Assert.Equal(new ProgressResponse(0, 0), created.Progress);
    }

    [Fact]
    public async Task Create_rejects_unknown_game_and_invalid_start_month()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var game = await CreateGameAsync(client, csrf, "Gloomhaven", "BoardGame");

        using var unknownGame = await CapexApi.PostJsonAsync(
            client,
            "/api/games/playthroughs",
            new CreatePlaythroughRequest("Run", game.Id + 999, 2026, 7, null, [], null),
            csrf);
        Assert.Equal(HttpStatusCode.BadRequest, unknownGame.StatusCode);
        Assert.Equal("games.playthrough.unknown_game", await CodeAsync(unknownGame));

        using var badMonth = await CapexApi.PostJsonAsync(
            client,
            "/api/games/playthroughs",
            new CreatePlaythroughRequest("Run", game.Id, 2026, 13, null, [], null),
            csrf);
        Assert.Equal(HttpStatusCode.BadRequest, badMonth.StatusCode);
        Assert.Equal("games.playthrough.validation", await CodeAsync(badMonth));
    }

    [Fact]
    public async Task Update_replaces_tags_and_reports_updated_metadata()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var game = await CreateGameAsync(client, csrf, "Hades", "PC");
        var created = await CreatePlaythroughAsync(
            client,
            csrf,
            new CreatePlaythroughRequest("First clear", game.Id, 2026, 1, null, ["Alpha", "Beta"], null));

        using var response = await CapexApi.PutJsonAsync(
            client,
            $"/api/games/playthroughs/{created.Id}",
            new UpdatePlaythroughRequest("First clear", game.Id, 2026, 3, "Active", ["Beta", "beta", "Gamma"], "Public"),
            csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = (await response.Content.ReadFromJsonAsync<PlaythroughResponse>(CancellationToken.None))!;
        Assert.Equal("Active", updated.Status);
        Assert.Equal(3, updated.StartMonth);
        Assert.Equal(["Beta", "Gamma"], updated.Tags);
    }

    [Fact]
    public async Task List_supports_search_filters_and_pagination()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var bg3 = await CreateGameAsync(client, csrf, "Baldur's Gate 3", "PC");
        var gloom = await CreateGameAsync(client, csrf, "Gloomhaven", "BoardGame");

        await CreatePlaythroughAsync(client, csrf, new CreatePlaythroughRequest("Durge", bg3.Id, 2026, 2, "Active", ["Evil"], null));
        await CreatePlaythroughAsync(client, csrf, new CreatePlaythroughRequest("Tavern run", gloom.Id, 2025, 11, "Planning", ["Coop"], null));

        // Search matches the linked game name, not only the playthrough name.
        var bySearch = await ListAsync(client, "search=baldur");
        Assert.Equal(["Durge"], bySearch.Items.Select(item => item.Name).ToArray());

        var byPlatform = await ListAsync(client, "platform=BoardGame");
        Assert.Equal(["Tavern run"], byPlatform.Items.Select(item => item.Name).ToArray());

        var byStatus = await ListAsync(client, "status=Active");
        Assert.Equal(["Durge"], byStatus.Items.Select(item => item.Name).ToArray());

        var byGame = await ListAsync(client, $"game={gloom.Id}");
        Assert.Equal(["Tavern run"], byGame.Items.Select(item => item.Name).ToArray());

        var byTag = await ListAsync(client, "tag=coop");
        Assert.Equal(["Tavern run"], byTag.Items.Select(item => item.Name).ToArray());

        var firstPage = await ListAsync(client, "pageSize=10&sort=name&sortDirection=asc&page=1");
        Assert.Equal(2, firstPage.TotalCount);
        Assert.Equal(["Durge", "Tavern run"], firstPage.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task List_sorts_by_derived_progress()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var game = await CreateGameAsync(client, csrf, "Celeste", "PC");
        var creatorId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        var complete = await CreatePlaythroughAsync(client, csrf, new CreatePlaythroughRequest("Full", game.Id, 2026, 1, null, [], null));
        var empty = await CreatePlaythroughAsync(client, csrf, new CreatePlaythroughRequest("Empty", game.Id, 2026, 1, null, [], null));
        await SeedProgressAsync(server, complete.Id, creatorId, totalGoals: 2, completedGoals: 2);

        var response = await ListAsync(client, "sort=progress&sortDirection=desc");
        Assert.Equal(["Full", "Empty"], response.Items.Select(item => item.Name).ToArray());
        var full = response.Items.Single(item => item.Id == complete.Id);
        Assert.Equal(new ProgressResponse(2, 2), full.Progress);
        Assert.Equal(new ProgressResponse(0, 0), response.Items.Single(item => item.Id == empty.Id).Progress);
    }

    [Fact]
    public async Task Delete_removes_playthrough_with_its_sections_goals_and_tags()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var game = await CreateGameAsync(client, csrf, "Outer Wilds", "PC");
        var creatorId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var created = await CreatePlaythroughAsync(
            client,
            csrf,
            new CreatePlaythroughRequest("Loop", game.Id, 2026, 5, null, ["Story"], null));
        await SeedProgressAsync(server, created.Id, creatorId, totalGoals: 2, completedGoals: 1);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/games/playthroughs/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await database.Set<Playthrough>().AnyAsync(item => item.Id == created.Id));
        Assert.False(await database.Set<PlaythroughTag>().AnyAsync(tag => tag.PlaythroughId == created.Id));
        Assert.False(await database.Set<Section>().AnyAsync(section => section.PlaythroughId == created.Id));
        Assert.Empty(await database.Set<Goal>().ToListAsync());
    }

    [Fact]
    public async Task Private_playthroughs_stay_isolated_and_public_ones_collaborate()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync(MemberName, MemberPassword);
        using var owner = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        using var member = await server.CreateAuthenticatedClientAsync(MemberName, MemberPassword);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var game = await CreateGameAsync(owner, ownerCsrf, "Stardew Valley", "PC");
        var secret = await CreatePlaythroughAsync(
            owner,
            ownerCsrf,
            new CreatePlaythroughRequest("Secret farm", game.Id, 2026, 4, null, [], "Private"));
        var shared = await CreatePlaythroughAsync(
            owner,
            ownerCsrf,
            new CreatePlaythroughRequest("Shared farm", game.Id, 2026, 4, null, [], "Public"));

        // The private playthrough is invisible to another user, including its detail.
        var memberList = await ListAsync(member, string.Empty);
        Assert.Equal(["Shared farm"], memberList.Items.Select(item => item.Name).ToArray());
        using var secretDetail = await member.GetAsync($"/api/games/playthroughs/{secret.Id}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, secretDetail.StatusCode);

        // A public playthrough is collaboratively editable by any authenticated user.
        using var collaborate = await CapexApi.PutJsonAsync(
            member,
            $"/api/games/playthroughs/{shared.Id}",
            new UpdatePlaythroughRequest("Shared farm", game.Id, 2026, 6, "Active", [], "Public"),
            memberCsrf);
        Assert.Equal(HttpStatusCode.OK, collaborate.StatusCode);

        // Only the creator may change visibility.
        using var forbidden = await CapexApi.PutJsonAsync(
            member,
            $"/api/games/playthroughs/{shared.Id}",
            new UpdatePlaythroughRequest("Shared farm", game.Id, 2026, 6, "Active", [], "Private"),
            memberCsrf);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("games.playthrough.visibility_forbidden", await CodeAsync(forbidden));
    }

    private static async Task<GameResponse> CreateGameAsync(HttpClient client, string csrf, string name, string platform)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/games/games", new CreateGameRequest(name, platform), csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GameResponse>(CancellationToken.None))!;
    }

    private static async Task<PlaythroughResponse> CreatePlaythroughAsync(
        HttpClient client,
        string csrf,
        CreatePlaythroughRequest request)
    {
        using var response = await CapexApi.PostJsonAsync(client, "/api/games/playthroughs", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlaythroughResponse>(CancellationToken.None))!;
    }

    private static async Task<PaginatedResponse<PlaythroughSummaryResponse>> ListAsync(HttpClient client, string query)
    {
        var route = query.Length == 0 ? "/api/games/playthroughs" : $"/api/games/playthroughs?{query}";
        return (await client.GetFromJsonAsync<PaginatedResponse<PlaythroughSummaryResponse>>(route, CancellationToken.None))!;
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        return problem!.Code;
    }

    private static async Task SeedProgressAsync(
        CapexTestServer server,
        int playthroughId,
        int creatorId,
        int totalGoals,
        int completedGoals)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var now = new DateTimeOffset(2026, 7, 7, 10, 0, 0, TimeSpan.Zero);
        var section = Section.Create(playthroughId, "Story", "Blue", 0, new UserId(creatorId), now);
        database.Add(section);
        await database.SaveChangesAsync();

        for (var position = 0; position < totalGoals; position++)
        {
            var goal = Goal.Create(section.Id, $"Goal {position}", position, new UserId(creatorId), now);
            if (position < completedGoals)
            {
                goal.SetCompletion(true, new UserId(creatorId), now);
            }

            database.Add(goal);
        }

        await database.SaveChangesAsync();
    }

    private sealed record ProblemPayload(string? Code);
}
