using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Health;

public sealed class HealthAssociationEndpointTests
{
    [Fact]
    public async Task Symmetric_create_delete_idempotency_and_counts_share_one_join_row()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var diseaseId = await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "Flu");
        var medicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Ibuprofen");

        using var addFromDisease = await PostAssociationAsync(client, $"/api/health/diseases/{diseaseId}/medicines/{medicineId}", csrf);
        using var addFromMedicine = await PostAssociationAsync(client, $"/api/health/medicines/{medicineId}/diseases/{diseaseId}", csrf);
        var diseaseMedicines = await client.GetFromJsonAsync<IReadOnlyList<MedicineSummaryResponse>>(
            $"/api/health/diseases/{diseaseId}/medicines",
            CancellationToken.None);
        var medicineDiseases = await client.GetFromJsonAsync<IReadOnlyList<DiseaseSummaryResponse>>(
            $"/api/health/medicines/{medicineId}/diseases",
            CancellationToken.None);
        var diseasePage = await client.GetFromJsonAsync<PaginatedResponse<DiseaseSummaryResponse>>(
            "/api/health/diseases",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, addFromDisease.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, addFromMedicine.StatusCode);
        Assert.Equal(1, await HealthTestData.AssociationCountAsync(server.Services, diseaseId, medicineId));
        Assert.Equal("Ibuprofen", Assert.Single(diseaseMedicines!).Name);
        var diseaseSummary = Assert.Single(medicineDiseases!);
        Assert.Equal("Flu", diseaseSummary.Name);
        Assert.Equal(1, diseaseSummary.AssociatedMedicineCount);
        Assert.Equal(1, Assert.Single(diseasePage!.Items).AssociatedMedicineCount);

        using var deleteFromMedicine = await CapexApi.DeleteAsync(client, $"/api/health/medicines/{medicineId}/diseases/{diseaseId}", csrf);
        using var deleteFromDisease = await CapexApi.DeleteAsync(client, $"/api/health/diseases/{diseaseId}/medicines/{medicineId}", csrf);

        Assert.Equal(HttpStatusCode.NoContent, deleteFromMedicine.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteFromDisease.StatusCode);
        Assert.False(await HealthTestData.AssociationExistsAsync(server.Services, diseaseId, medicineId));
    }

    [Fact]
    public async Task Create_rejects_inaccessible_private_endpoint_without_disclosing_it()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateDiseaseId = await HealthTestData.SeedDiseaseAsync(
            server.Services,
            founderId,
            name: "Private disease",
            visibility: RecordVisibility.Private);
        var publicMedicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Public medicine");

        await server.CreateUserAsync("health-association-member", "HealthMember123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "health-association-member", "HealthMember123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var response = await PostAssociationAsync(
            member,
            $"/api/health/diseases/{privateDiseaseId}/medicines/{publicMedicineId}",
            csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("health.association.not_accessible", problem!.Code);
        Assert.False(await HealthTestData.AssociationExistsAsync(server.Services, privateDiseaseId, publicMedicineId));
    }

    [Fact]
    public async Task Viewer_filtered_reads_and_mutations_isolate_private_links_per_user()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var diseaseId = await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "Shared disease");
        var publicMedicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Public medicine");
        var founderPrivateMedicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Founder private medicine",
            visibility: RecordVisibility.Private);
        await HealthTestData.SeedAssociationAsync(server.Services, diseaseId, publicMedicineId);
        await HealthTestData.SeedAssociationAsync(server.Services, diseaseId, founderPrivateMedicineId);

        await server.CreateUserAsync("health-association-owner", "HealthMember123!");
        var memberId = await server.GetUserIdAsync("health-association-owner");
        var memberPrivateMedicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            memberId,
            name: "Member private medicine",
            visibility: RecordVisibility.Private);
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "health-association-owner", "HealthMember123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var addMemberPrivate = await PostAssociationAsync(
            member,
            $"/api/health/diseases/{diseaseId}/medicines/{memberPrivateMedicineId}",
            memberCsrf);
        var memberView = await member.GetFromJsonAsync<IReadOnlyList<MedicineSummaryResponse>>(
            $"/api/health/diseases/{diseaseId}/medicines",
            CancellationToken.None);
        var memberDiseasePage = await member.GetFromJsonAsync<PaginatedResponse<DiseaseSummaryResponse>>(
            "/api/health/diseases",
            CancellationToken.None);

        using var founderClient = await server.CreateAuthenticatedClientAsync();
        var founderView = await founderClient.GetFromJsonAsync<IReadOnlyList<MedicineSummaryResponse>>(
            $"/api/health/diseases/{diseaseId}/medicines",
            CancellationToken.None);
        var founderDiseasePage = await founderClient.GetFromJsonAsync<PaginatedResponse<DiseaseSummaryResponse>>(
            "/api/health/diseases",
            CancellationToken.None);
        using var removeMemberPrivate = await CapexApi.DeleteAsync(
            member,
            $"/api/health/diseases/{diseaseId}/medicines/{memberPrivateMedicineId}",
            memberCsrf);

        Assert.Equal(HttpStatusCode.NoContent, addMemberPrivate.StatusCode);
        Assert.Equal(["Member private medicine", "Public medicine"], memberView!.Select(medicine => medicine.Name).ToArray());
        Assert.Equal(2, Assert.Single(memberDiseasePage!.Items).AssociatedMedicineCount);
        Assert.Equal(["Founder private medicine", "Public medicine"], founderView!.Select(medicine => medicine.Name).ToArray());
        Assert.Equal(2, Assert.Single(founderDiseasePage!.Items).AssociatedMedicineCount);
        Assert.Equal(HttpStatusCode.NoContent, removeMemberPrivate.StatusCode);
        Assert.True(await HealthTestData.AssociationExistsAsync(server.Services, diseaseId, founderPrivateMedicineId));
        Assert.False(await HealthTestData.AssociationExistsAsync(server.Services, diseaseId, memberPrivateMedicineId));
    }

    [Fact]
    public async Task Publish_guard_rejects_private_to_public_when_linked_to_non_public_record()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var diseaseId = await HealthTestData.SeedDiseaseAsync(
            server.Services,
            founderId,
            name: "Private disease",
            visibility: RecordVisibility.Private);
        var medicineId = await HealthTestData.SeedMedicineAsync(
            server.Services,
            founderId,
            name: "Private medicine",
            visibility: RecordVisibility.Private);
        await HealthTestData.SeedAssociationAsync(server.Services, diseaseId, medicineId);
        var diseaseCategoryId = await HealthTestData.DiseaseCategoryIdAsync(server.Services, "Other");
        var medicineCategoryId = await HealthTestData.MedicineCategoryIdAsync(server.Services, "Other");

        using var publishDisease = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/diseases/{diseaseId}",
            new UpdateDiseaseRequest("Private disease", diseaseCategoryId, null, null, null, "Public"),
            csrf);
        using var publishMedicine = await CapexApi.PutJsonAsync(
            client,
            $"/api/health/medicines/{medicineId}",
            new UpdateMedicineRequest("Private medicine", medicineCategoryId, null, false, null, null, "Public"),
            csrf);

        var diseaseProblem = await publishDisease.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var medicineProblem = await publishMedicine.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, publishDisease.StatusCode);
        Assert.Equal("health.association.publish_blocked", diseaseProblem!.Code);
        Assert.Contains("1", diseaseProblem.Title, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Conflict, publishMedicine.StatusCode);
        Assert.Equal("health.association.publish_blocked", medicineProblem!.Code);
        Assert.Contains("1", medicineProblem.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Deleting_disease_or_medicine_cleans_join_rows()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var firstDiseaseId = await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "First disease");
        var firstMedicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "First medicine");
        await HealthTestData.SeedAssociationAsync(server.Services, firstDiseaseId, firstMedicineId);
        var secondDiseaseId = await HealthTestData.SeedDiseaseAsync(server.Services, founderId, name: "Second disease");
        var secondMedicineId = await HealthTestData.SeedMedicineAsync(server.Services, founderId, name: "Second medicine");
        await HealthTestData.SeedAssociationAsync(server.Services, secondDiseaseId, secondMedicineId);

        using var deleteDisease = await CapexApi.DeleteAsync(client, $"/api/health/diseases/{firstDiseaseId}", csrf);
        using var deleteMedicine = await CapexApi.DeleteAsync(client, $"/api/health/medicines/{secondMedicineId}", csrf);

        Assert.Equal(HttpStatusCode.NoContent, deleteDisease.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteMedicine.StatusCode);
        Assert.False(await HealthTestData.AssociationExistsAsync(server.Services, firstDiseaseId, firstMedicineId));
        Assert.False(await HealthTestData.AssociationExistsAsync(server.Services, secondDiseaseId, secondMedicineId));
    }

    private static async Task<HttpResponseMessage> PostAssociationAsync(HttpClient client, string route, string csrf) =>
        await CapexApi.PostJsonAsync(client, route, new { }, csrf);

    private sealed record ProblemPayload(string? Code, string Title);
}
