using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Health;

public sealed class HealthDiseaseEndpointTests
{
    [Fact]
    public async Task Diseases_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/health/diseases", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_defaults_and_optional_fields()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await HealthTestData.DiseaseCategoryIdAsync(server.Services, "Acute");

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/health/diseases",
            new CreateDiseaseRequest("  Flu  ", categoryId, null, null, null, null),
            csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DiseaseResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Flu", created.Name);
        Assert.Equal(categoryId, created.CategoryId);
        Assert.Equal("Acute", created.CategoryName);
        Assert.Null(created.Symptoms);
        Assert.Null(created.AverageDurationDays);
        Assert.Null(created.Notes);
        Assert.Equal("Public", created.Visibility);
    }

    [Fact]
    public async Task List_supports_pagination_search_exact_filters_and_sorting()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await HealthTestData.SeedDiseaseAsync(
            server.Services,
            founderId,
            name: "Flu",
            categoryName: "Infection",
            symptoms: "Fever",
            averageDurationDays: 7);
        await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "Hay fever", categoryName: "Allergy", symptoms: "Sneezing");
        await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "Migraine", categoryName: "Chronic", visibility: RecordVisibility.Private);

        var allergyId = await HealthTestData.DiseaseCategoryIdAsync(server.Services, "Allergy");

        var firstPage = await GetPageAsync(client, "/api/health/diseases?page=1&pageSize=2");
        var search = await GetPageAsync(client, "/api/health/diseases?search=HAY");
        var byCategory = await GetPageAsync(client, $"/api/health/diseases?category={allergyId}");
        var privateOnly = await GetPageAsync(client, "/api/health/diseases?visibility=Private");
        var byCategorySort = await GetPageAsync(client, "/api/health/diseases?pageSize=10&sort=category&sortDirection=asc");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal("Hay fever", Assert.Single(search.Items).Name);
        Assert.Equal("Hay fever", Assert.Single(byCategory.Items).Name);
        Assert.Equal("Migraine", Assert.Single(privateOnly.Items).Name);
        Assert.Equal(["Allergy", "Chronic", "Infection"], byCategorySort.Items.Select(item => item.CategoryName).ToArray());
    }

    [Fact]
    public async Task Detail_update_and_delete_manage_the_complete_disease()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var diseaseId = await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "Original", symptoms: "Old");
        var categoryId = await HealthTestData.DiseaseCategoryIdAsync(server.Services, "Chronic");

        var detail = await client.GetFromJsonAsync<DiseaseResponse>($"/api/health/diseases/{diseaseId}", CancellationToken.None);
        using var update = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/diseases/{diseaseId}",
            new UpdateDiseaseRequest("Updated migraine", categoryId, "Headache", 3, "Rest", "Private"),
            csrf);
        var updated = await update.Content.ReadFromJsonAsync<DiseaseResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/health/diseases/{diseaseId}", csrf);

        Assert.NotNull(detail);
        Assert.Equal("Original", detail.Name);
        Assert.Equal("Old", detail.Symptoms);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated migraine", updated!.Name);
        Assert.Equal(categoryId, updated.CategoryId);
        Assert.Equal("Headache", updated.Symptoms);
        Assert.Equal(3, updated.AverageDurationDays);
        Assert.Equal("Rest", updated.Notes);
        Assert.Equal("Private", updated.Visibility);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await HealthTestData.DiseaseExistsAsync(server.Services, diseaseId));
    }

    [Fact]
    public async Task Unknown_references_and_invalid_values_return_disease_problems()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await HealthTestData.DiseaseCategoryIdAsync(server.Services, "Acute");

        using var unknownCategory = await CapexApi.PostJsonAsync(
            client,
            "/api/health/diseases",
            new CreateDiseaseRequest("Flu", 999_999, null, null, null, null),
            csrf);
        using var invalidName = await CapexApi.PostJsonAsync(
            client,
            "/api/health/diseases",
            new CreateDiseaseRequest("   ", categoryId, null, null, null, null),
            csrf);
        using var invalidDuration = await CapexApi.PostJsonAsync(
            client,
            "/api/health/diseases",
            new CreateDiseaseRequest("Flu", categoryId, null, 100_001, null, null),
            csrf);

        var unknownProblem = await unknownCategory.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidNameProblem = await invalidName.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var invalidDurationProblem = await invalidDuration.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, unknownCategory.StatusCode);
        Assert.Equal("health.catalog.unknown_reference", unknownProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidName.StatusCode);
        Assert.Equal("health.disease.validation", invalidNameProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, invalidDuration.StatusCode);
        Assert.Equal("health.disease.validation", invalidDurationProblem!.Code);
    }

    [Fact]
    public async Task Public_collaboration_private_isolation_and_not_found_privacy_follow_visibility_rules()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicDiseaseId = await HealthTestData.SeedDiseaseAsync(
            server.Services,
            founderId,
            name: "Shared",
            visibility: RecordVisibility.Public);
        var privateDiseaseId = await HealthTestData.SeedDiseaseAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        var categoryId = await HealthTestData.DiseaseCategoryIdAsync(server.Services, "Other");

        await server.CreateUserAsync("health-member", "HealthMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "health-member", "HealthMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        var privateDetail = await member.GetAsync($"/api/health/diseases/{privateDiseaseId}", CancellationToken.None);
        using var editPublic = await CapexApi.PutJsonAsync(
            member,
            $"/api/health/diseases/{publicDiseaseId}",
            new UpdateDiseaseRequest("Shared edited", categoryId, null, null, null, null),
            memberCsrf);
        using var editPrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/health/diseases/{privateDiseaseId}",
            new UpdateDiseaseRequest("Private edited", categoryId, null, null, null, "Private"),
            memberCsrf);
        using var makePrivate = await CapexApi.PutJsonAsync(
            member,
            $"/api/health/diseases/{publicDiseaseId}",
            new UpdateDiseaseRequest("Shared hidden", categoryId, null, null, null, "Private"),
            memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, privateDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, editPublic.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, editPrivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, makePrivate.StatusCode);
    }

    private static async Task<PaginatedResponse<DiseaseSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<DiseaseSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code);
}
