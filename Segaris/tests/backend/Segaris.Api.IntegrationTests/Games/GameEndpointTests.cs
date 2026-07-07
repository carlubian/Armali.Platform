using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Games;

public sealed class GameEndpointTests
{
    [Fact]
    public async Task Games_require_authentication_and_start_empty()
    {
        using var server = new CapexTestServer();
        using var anonymous = server.CreateClient();

        using var unauthorized = await anonymous.GetAsync("/api/games/games", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var client = await server.CreateAuthenticatedClientAsync();
        var games = await client.GetFromJsonAsync<GameResponse[]>("/api/games/games", CancellationToken.None);
        Assert.NotNull(games);
        Assert.Empty(games);
    }

    [Fact]
    public async Task Management_routes_reject_normal_users()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/games/games",
            new CreateGameRequest("Anything", "PC"),
            csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_the_last_unreferenced_game()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var first = await CreateAsync(client, csrf, "Baldur's Gate 3", "PC");
        var second = await CreateAsync(client, csrf, "Gloomhaven", "BoardGame");
        Assert.Equal((0, 1), (first.SortOrder, second.SortOrder));

        using var moved = await CapexApi.PostJsonAsync(
            client,
            $"/api/games/games/{second.Id}/move",
            new CatalogMoveRequest("up"),
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);

        var afterMove = await client.GetFromJsonAsync<GameResponse[]>("/api/games/games", CancellationToken.None);
        Assert.NotNull(afterMove);
        Assert.Equal([second.Id, first.Id], afterMove.Select(game => game.Id).ToArray());
        Assert.Equal(Enumerable.Range(0, afterMove.Length), afterMove.Select(game => game.SortOrder));

        using var deleteFirst = await CapexApi.DeleteAsync(client, $"/api/games/games/{first.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleteFirst.StatusCode);
        using var deleteSecond = await CapexApi.DeleteAsync(client, $"/api/games/games/{second.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleteSecond.StatusCode);

        var empty = await client.GetFromJsonAsync<GameResponse[]>("/api/games/games", CancellationToken.None);
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Duplicate_game_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        await CreateAsync(client, csrf, "Gloomhaven", "BoardGame");
        using var duplicate = await CapexApi.PostJsonAsync(
            client,
            "/api/games/games",
            new CreateGameRequest("  gloomhaven  ", "BoardGame"),
            csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("games.game.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Referenced_game_requires_replacement_and_repoints_playthroughs_atomically()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var source = await CreateAsync(client, csrf, "Baldur's Gate 3", "PC");
        var replacement = await CreateAsync(client, csrf, "Gloomhaven", "BoardGame");
        var userId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            database.Add(Playthrough.Create(
                new PlaythroughValues("Run", source.Id, 2026, 7, "Active", [], "Public"),
                new UserId(userId),
                new DateTimeOffset(2026, 7, 7, 10, 0, 0, TimeSpan.Zero)));
            await database.SaveChangesAsync();
        }

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/games/games/{source.Id}/deletion-impact",
            CancellationToken.None);
        Assert.True(impact!.IsReferenced);
        Assert.False(impact.CanDeleteDirectly);
        Assert.False(impact.CanClearReferences);
        Assert.True(impact.HasReplacementCandidates);

        using var directDelete = await CapexApi.DeleteAsync(client, $"/api/games/games/{source.Id}", csrf);
        Assert.Equal(HttpStatusCode.Conflict, directDelete.StatusCode);

        using var replaced = await CapexApi.PostJsonAsync(
            client,
            $"/api/games/games/{source.Id}/replace-and-delete",
            new CatalogReplacementRequest(replacement.Id, ClearReferences: false, ExchangeRate: null),
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, replaced.StatusCode);

        await using (var scope = server.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var playthrough = await database.Set<Playthrough>().SingleAsync();
            Assert.Equal(replacement.Id, playthrough.GameId);
            Assert.False(await database.Set<Game>().AnyAsync(game => game.Id == source.Id));
        }
    }

    private static async Task<GameResponse> CreateAsync(HttpClient client, string csrf, string name, string platform)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/games/games",
            new CreateGameRequest(name, platform),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GameResponse>(CancellationToken.None))!;
    }

    private sealed record ProblemPayload(string? Code);
}
