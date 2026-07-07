using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Games.Contracts;
using Segaris.Api.Modules.Games.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;

namespace Segaris.Api.IntegrationTests.Games;

public sealed class SectionGoalEndpointTests
{
    private const string MemberName = "member";
    private const string MemberPassword = "MemberPass123!";

    [Fact]
    public async Task Sections_can_be_created_updated_reordered_and_listed_with_progress()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var playthrough = await CreatePlaythroughAsync(client, csrf, "Campaign");

        var empty = await ListSectionsAsync(client, playthrough.Id);
        Assert.Empty(empty);

        var story = await CreateSectionAsync(client, csrf, playthrough.Id, "Story", "Blue");
        var collect = await CreateSectionAsync(client, csrf, playthrough.Id, "Collectibles", "Amber");

        using var duplicate = await CapexApi.PostJsonAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections",
            new CreateSectionRequest(" story ", "Green"),
            csrf);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("games.section.duplicate_name", await CodeAsync(duplicate));

        using var invalidColor = await CapexApi.PostJsonAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections",
            new CreateSectionRequest("Bosses", "Gold"),
            csrf);
        Assert.Equal(HttpStatusCode.BadRequest, invalidColor.StatusCode);
        Assert.Equal("games.section.validation", await CodeAsync(invalidColor));

        using var reorder = await CapexApi.PutJsonAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections/order",
            new SectionOrderRequest([collect.Id, story.Id]),
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, reorder.StatusCode);

        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections/{story.Id}",
            new UpdateSectionRequest("Main Story", "Green"),
            csrf);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<SectionResponse>(CancellationToken.None))!;
        Assert.Equal(("Main Story", "Green"), (updated.Name, updated.Color));

        var sections = await ListSectionsAsync(client, playthrough.Id);
        Assert.Equal([collect.Id, story.Id], sections.Select(section => section.Id).ToArray());
        Assert.Equal([0, 1], sections.Select(section => section.SortOrder).ToArray());
        Assert.All(sections, section => Assert.Equal(new ProgressResponse(0, 0), section.Progress));
    }

    [Fact]
    public async Task Goals_keep_creation_order_and_recompute_section_and_playthrough_progress()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var playthrough = await CreatePlaythroughAsync(client, csrf, "Progress run");
        var section = await CreateSectionAsync(client, csrf, playthrough.Id, "Story", "Purple");

        var first = await CreateGoalAsync(client, csrf, playthrough.Id, section.Id, "Find the ship");
        var second = await CreateGoalAsync(client, csrf, playthrough.Id, section.Id, "Launch");

        using var complete = await CapexApi.PutJsonAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections/{section.Id}/goals/{second.Id}/completion",
            new GoalCompletionRequest(true),
            csrf);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var completed = (await complete.Content.ReadFromJsonAsync<GoalResponse>(CancellationToken.None))!;
        Assert.True(completed.Completed);
        Assert.Equal(second.Position, completed.Position);

        using var edit = await CapexApi.PutJsonAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections/{section.Id}/goals/{first.Id}",
            new UpdateGoalRequest("Find the launch codes"),
            csrf);
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        var goals = await ListGoalsAsync(client, playthrough.Id, section.Id);
        Assert.Equal([first.Id, second.Id], goals.Select(goal => goal.Id).ToArray());
        Assert.Equal([0, 1], goals.Select(goal => goal.Position).ToArray());
        Assert.Equal("Find the launch codes", goals[0].Text);

        var sectionDetail = await client.GetFromJsonAsync<SectionResponse>(
            $"/api/games/playthroughs/{playthrough.Id}/sections/{section.Id}",
            CancellationToken.None);
        Assert.Equal(new ProgressResponse(1, 2), sectionDetail!.Progress);

        var playthroughDetail = await client.GetFromJsonAsync<PlaythroughResponse>(
            $"/api/games/playthroughs/{playthrough.Id}",
            CancellationToken.None);
        Assert.Equal(new ProgressResponse(1, 2), playthroughDetail!.Progress);

        using var deleted = await CapexApi.DeleteAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections/{section.Id}/goals/{first.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<PlaythroughResponse>(
            $"/api/games/playthroughs/{playthrough.Id}",
            CancellationToken.None);
        Assert.Equal(new ProgressResponse(1, 1), afterDelete!.Progress);
    }

    [Fact]
    public async Task Section_delete_removes_owned_goals()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var playthrough = await CreatePlaythroughAsync(client, csrf, "Cleanup run");
        var section = await CreateSectionAsync(client, csrf, playthrough.Id, "Cleanup", "Slate");
        await CreateGoalAsync(client, csrf, playthrough.Id, section.Id, "Temporary goal");

        using var deleted = await CapexApi.DeleteAsync(
            client,
            $"/api/games/playthroughs/{playthrough.Id}/sections/{section.Id}",
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await database.Set<Section>().AnyAsync(item => item.Id == section.Id));
        Assert.Empty(await database.Set<Goal>().ToListAsync());
    }

    [Fact]
    public async Task Section_and_goal_routes_inherit_visibility_and_parent_scope()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync(MemberName, MemberPassword);
        using var owner = await server.CreateAuthenticatedClientAsync();
        var ownerCsrf = await CapexTestServer.GetCsrfTokenAsync(owner);
        using var member = await server.CreateAuthenticatedClientAsync(MemberName, MemberPassword);
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var secret = await CreatePlaythroughAsync(owner, ownerCsrf, "Secret", visibility: "Private");
        var shared = await CreatePlaythroughAsync(owner, ownerCsrf, "Shared");
        var other = await CreatePlaythroughAsync(owner, ownerCsrf, "Other");
        var sharedSection = await CreateSectionAsync(owner, ownerCsrf, shared.Id, "Shared Section", "Teal");
        var otherSection = await CreateSectionAsync(owner, ownerCsrf, other.Id, "Other Section", "Red");
        var goal = await CreateGoalAsync(owner, ownerCsrf, shared.Id, sharedSection.Id, "Shared goal");

        using var hiddenSections = await member.GetAsync(
            $"/api/games/playthroughs/{secret.Id}/sections",
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, hiddenSections.StatusCode);
        Assert.Equal("games.section.not_found", await CodeAsync(hiddenSections));

        using var collaborated = await CapexApi.PostJsonAsync(
            member,
            $"/api/games/playthroughs/{shared.Id}/sections/{sharedSection.Id}/goals",
            new CreateGoalRequest("Member goal"),
            memberCsrf);
        Assert.Equal(HttpStatusCode.Created, collaborated.StatusCode);

        using var mismatchedSection = await member.GetAsync(
            $"/api/games/playthroughs/{shared.Id}/sections/{otherSection.Id}/goals",
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, mismatchedSection.StatusCode);
        Assert.Equal("games.section.not_found", await CodeAsync(mismatchedSection));

        using var mismatchedGoal = await CapexApi.PutJsonAsync(
            member,
            $"/api/games/playthroughs/{other.Id}/sections/{otherSection.Id}/goals/{goal.Id}",
            new UpdateGoalRequest("Wrong parent"),
            memberCsrf);
        Assert.Equal(HttpStatusCode.NotFound, mismatchedGoal.StatusCode);
        Assert.Equal("games.goal.not_found", await CodeAsync(mismatchedGoal));
    }

    [Fact]
    public async Task Reorder_rejects_partial_duplicate_or_foreign_section_sets()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var playthrough = await CreatePlaythroughAsync(client, csrf, "Order run");
        var other = await CreatePlaythroughAsync(client, csrf, "Other run");
        var first = await CreateSectionAsync(client, csrf, playthrough.Id, "First", "Blue");
        var second = await CreateSectionAsync(client, csrf, playthrough.Id, "Second", "Green");
        var foreign = await CreateSectionAsync(client, csrf, other.Id, "Foreign", "Red");

        foreach (var ids in new[] { new[] { first.Id }, [first.Id, first.Id], [first.Id, foreign.Id] })
        {
            using var response = await CapexApi.PutJsonAsync(
                client,
                $"/api/games/playthroughs/{playthrough.Id}/sections/order",
                new SectionOrderRequest(ids),
                csrf);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("games.section.invalid_order", await CodeAsync(response));
        }

        var sections = await ListSectionsAsync(client, playthrough.Id);
        Assert.Equal([first.Id, second.Id], sections.Select(section => section.Id).ToArray());
    }

    private static async Task<GameResponse> CreateGameAsync(HttpClient client, string csrf)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/games/games",
            new CreateGameRequest($"Game {Guid.NewGuid():N}", "PC"),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GameResponse>(CancellationToken.None))!;
    }

    private static async Task<PlaythroughResponse> CreatePlaythroughAsync(
        HttpClient client,
        string csrf,
        string name,
        string? visibility = null)
    {
        var game = await CreateGameAsync(client, csrf);
        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/games/playthroughs",
            new CreatePlaythroughRequest(name, game.Id, 2026, 7, null, [], visibility),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlaythroughResponse>(CancellationToken.None))!;
    }

    private static async Task<SectionResponse> CreateSectionAsync(
        HttpClient client,
        string csrf,
        int playthroughId,
        string name,
        string color)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/games/playthroughs/{playthroughId}/sections",
            new CreateSectionRequest(name, color),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SectionResponse>(CancellationToken.None))!;
    }

    private static async Task<GoalResponse> CreateGoalAsync(
        HttpClient client,
        string csrf,
        int playthroughId,
        int sectionId,
        string text)
    {
        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals",
            new CreateGoalRequest(text),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GoalResponse>(CancellationToken.None))!;
    }

    private static async Task<IReadOnlyList<SectionResponse>> ListSectionsAsync(HttpClient client, int playthroughId) =>
        (await client.GetFromJsonAsync<IReadOnlyList<SectionResponse>>(
            $"/api/games/playthroughs/{playthroughId}/sections",
            CancellationToken.None))!;

    private static async Task<IReadOnlyList<GoalResponse>> ListGoalsAsync(
        HttpClient client,
        int playthroughId,
        int sectionId) =>
        (await client.GetFromJsonAsync<IReadOnlyList<GoalResponse>>(
            $"/api/games/playthroughs/{playthroughId}/sections/{sectionId}/goals",
            CancellationToken.None))!;

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        return problem!.Code;
    }

    private sealed record ProblemPayload(string? Code);
}
