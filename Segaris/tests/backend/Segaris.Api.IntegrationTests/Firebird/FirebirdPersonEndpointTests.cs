using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Firebird;

public sealed class FirebirdPersonEndpointTests
{
    [Fact]
    public async Task People_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/people", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_and_trims_values()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await FirebirdTestData.CategoryIdAsync(server.Services, "Friend");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/people",
            FirebirdPersonRequestBuilder.Default()
                .WithName("  Ada Lovelace  ")
                .WithCategory(categoryId)
                .WithStatus(null)
                .WithBirthday(null, null)
                .WithNotes("  First programmer  ")
                .WithVisibility(null)
                .BuildCreate(),
            csrf);
        var created = await response.Content.ReadFromJsonAsync<PersonResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Ada Lovelace", created.Name);
        Assert.Equal("Friend", created.CategoryName);
        Assert.Equal("Unknown", created.Status);
        Assert.Null(created.BirthdayMonth);
        Assert.Null(created.BirthdayDay);
        Assert.Equal("First programmer", created.Notes);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal("placeholder", created.Avatar.Source);
        Assert.Empty(created.Usernames);
        Assert.Empty(created.Interactions);
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_and_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "Zoe Zebra",
            categoryName: "Friend",
            status: PersonStatus.Active,
            birthdayMonth: 12,
            birthdayDay: 20,
            notes: "met at chess club");
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "Alice Alpha",
            categoryName: "Family",
            status: PersonStatus.Blocked,
            birthdayMonth: 1,
            birthdayDay: 5);
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "Bob Beta",
            categoryName: "Colleague",
            status: PersonStatus.Unavailable,
            visibility: RecordVisibility.Private);
        await FirebirdTestData.SeedPersonAsync(
            server.Services,
            founderId,
            name: "No Birthday",
            categoryName: "Other");

        var familyId = await FirebirdTestData.CategoryIdAsync(server.Services, "Family");

        var firstPage = await GetPageAsync(client, "/api/people?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/people?search=CHESS");
        var byCategory = await GetPageAsync(client, $"/api/people?category={familyId}");
        var byStatus = await GetPageAsync(client, "/api/people?status=Blocked");
        var byCreator = await GetPageAsync(client, $"/api/people?creator={founderId}");
        var privateOnly = await GetPageAsync(client, "/api/people?visibility=Private");
        var birthdaySort = await GetPageAsync(client, "/api/people?sort=birthday&sortDirection=asc");

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal("Zoe Zebra", Assert.Single(search.Items).Name);
        Assert.Equal("Alice Alpha", Assert.Single(byCategory.Items).Name);
        Assert.Equal("Alice Alpha", Assert.Single(byStatus.Items).Name);
        Assert.Equal(4, byCreator.TotalCount);
        Assert.Equal("Bob Beta", Assert.Single(privateOnly.Items).Name);
        Assert.Equal(["Alice Alpha", "Zoe Zebra", "Bob Beta", "No Birthday"], birthdaySort.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Detail_update_and_delete_manage_the_complete_person()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var personId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Original");
        var categoryId = await FirebirdTestData.CategoryIdAsync(server.Services, "Family");

        var detail = await client.GetFromJsonAsync<PersonResponse>($"/api/people/{personId}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/people/{personId}",
            FirebirdPersonRequestBuilder.Default()
                .WithName("Updated person")
                .WithCategory(categoryId)
                .WithStatus("Unavailable")
                .WithBirthday(2, 29)
                .WithNotes("Close relative")
                .WithVisibility("Private")
                .BuildUpdate(),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<PersonResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/people/{personId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Name);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated person", updated!.Name);
        Assert.Equal("Family", updated.CategoryName);
        Assert.Equal("Unavailable", updated.Status);
        Assert.Equal(2, updated.BirthdayMonth);
        Assert.Equal(29, updated.BirthdayDay);
        Assert.Equal("Close relative", updated.Notes);
        Assert.Equal("Private", updated.Visibility);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await FirebirdTestData.PersonExistsAsync(server.Services, personId));
    }

    [Fact]
    public async Task Unknown_references_and_invalid_values_return_person_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await FirebirdTestData.CategoryIdAsync(server.Services, "Friend");

        using var unknown = await CapexApi.PostJsonAsync(
            client,
            "/api/people",
            FirebirdPersonRequestBuilder.Default().WithCategory(999_999).BuildCreate(),
            csrf);
        using var invalid = await CapexApi.PostJsonAsync(
            client,
            "/api/people",
            FirebirdPersonRequestBuilder.Default().WithCategory(categoryId).WithBirthday(2, null).BuildCreate(),
            csrf);

        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidProblem = await invalid.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("firebird.catalog.unknown_reference", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("firebird.person.validation", invalidProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_and_private_isolation_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicPersonId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Shared", visibility: RecordVisibility.Public);
        var privatePersonId = await FirebirdTestData.SeedPersonAsync(server.Services, founderId, name: "Private", visibility: RecordVisibility.Private);
        var categoryId = await FirebirdTestData.CategoryIdAsync(server.Services, "Other");

        await server.CreateUserAsync("firebird-member", "FirebirdMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "firebird-member", "FirebirdMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/people/{publicPersonId}",
            FirebirdPersonRequestBuilder.Default().WithName("Shared edited").WithCategory(categoryId).BuildUpdate(),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/people/{privatePersonId}",
            FirebirdPersonRequestBuilder.Default().WithName("Private edited").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/people/{publicPersonId}",
            FirebirdPersonRequestBuilder.Default().WithName("Shared hidden").WithCategory(categoryId).WithVisibility("Private").BuildUpdate(),
            memberCsrf);

        var memberPage = await GetPageAsync(member, "/api/people?sort=name");
        using var memberMissingDetail = await member.GetAsync($"/api/people/{privatePersonId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
        Assert.DoesNotContain(memberPage.Items, item => item.Name == "Private");
        Assert.Equal(HttpStatusCode.NotFound, memberMissingDetail.StatusCode);
    }

    private static async Task<PaginatedResponse<PersonSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<PersonSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}
