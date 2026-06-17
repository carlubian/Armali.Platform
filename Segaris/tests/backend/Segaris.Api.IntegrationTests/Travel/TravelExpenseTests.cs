using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelExpenseTests
{
    [Fact]
    public async Task Create_applies_defaults_and_detail_reports_shared_catalog_names()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);
        var defaultCategoryId = await TravelTestData.ExpenseCategoryIdAsync(server.Services, "Flight");
        var currencyId = await TravelTestData.CurrencyIdAsync(server.Services);
        var supplierId = await TravelTestData.SupplierIdAsync(server.Services);
        var costCenterId = await TravelTestData.CostCenterIdAsync(server.Services);
        var request = new CreateTravelExpenseRequest(
            ExpenseCategoryId: 0,
            Description: "  Taxi from airport  ",
            Date: default,
            Amount: 0m,
            CurrencyId: currencyId,
            SupplierId: supplierId,
            CostCenterId: costCenterId,
            Notes: "  Receipt in email  ");

        using var response = await CapexApi.PostJsonAsync(client, $"/api/travel/trips/{tripId}/expenses", request, csrf);
        var created = await response.Content.ReadFromJsonAsync<TravelExpenseResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(defaultCategoryId, created.ExpenseCategoryId);
        Assert.Equal("Taxi from airport", created.Description);
        Assert.Equal(TodayInMadrid(), created.Date);
        Assert.Equal(0m, created.Amount);
        Assert.Equal(ConfigurationCatalog.CurrencyCodes.Default, created.CurrencyCode);
        Assert.Equal("Amazon", created.SupplierName);
        Assert.Equal("Household", created.CostCenterName);
        Assert.Equal("Receipt in email", created.Notes);
        Assert.Empty(created.Attachments);
    }

    [Fact]
    public async Task Create_requires_antiforgery_currency_and_valid_catalog_references()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);
        var categoryId = await TravelTestData.ExpenseCategoryIdAsync(server.Services);
        var valid = DefaultCreateRequest(categoryId, await TravelTestData.CurrencyIdAsync(server.Services));
        var unknownCurrency = valid with { CurrencyId = 999_999 };

        using var withoutCsrf = await CapexApi.PostJsonAsync(client, $"/api/travel/trips/{tripId}/expenses", valid, csrf: null);
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var unknown = await CapexApi.PostJsonAsync(client, $"/api/travel/trips/{tripId}/expenses", unknownCurrency, csrf);
        var problem = await unknown.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("travel.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Create_and_update_reject_validation_failures()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);
        var categoryId = await TravelTestData.ExpenseCategoryIdAsync(server.Services);
        var currencyId = await TravelTestData.CurrencyIdAsync(server.Services);
        var badDescription = DefaultCreateRequest(categoryId, currencyId) with { Description = " " };
        var badAmount = DefaultCreateRequest(categoryId, currencyId) with { Amount = 1.999m };

        using var descriptionResponse = await CapexApi.PostJsonAsync(client, $"/api/travel/trips/{tripId}/expenses", badDescription, csrf);
        using var amountResponse = await CapexApi.PostJsonAsync(client, $"/api/travel/trips/{tripId}/expenses", badAmount, csrf);
        var descriptionProblem = await descriptionResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var amountProblem = await amountResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, descriptionResponse.StatusCode);
        Assert.Equal("travel.expense.validation", descriptionProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, amountResponse.StatusCode);
        Assert.Equal("travel.expense.validation", amountProblem!.Code);
    }

    [Fact]
    public async Task List_supports_trip_scoped_filters_sorting_and_pagination()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);
        var otherTripId = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Other");
        var eur = await TravelTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.Euro);
        var usd = await TravelTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.UsDollar);
        var meals = await TravelTestData.ExpenseCategoryIdAsync(server.Services, "Meals");
        var lodging = await TravelTestData.ExpenseCategoryIdAsync(server.Services, "Lodging");
        await SeedExpenseAsync(server.Services, tripId, founderId, meals, eur, "Airport meal", new DateOnly(2026, 6, 2), 30m);
        await SeedExpenseAsync(server.Services, tripId, founderId, lodging, usd, "Hotel", new DateOnly(2026, 6, 1), 120m);
        await SeedExpenseAsync(server.Services, otherTripId, founderId, meals, eur, "Other meal", new DateOnly(2026, 6, 3), 10m);

        var result = await client.GetFromJsonAsync<PaginatedResponse<TravelExpenseSummaryResponse>>(
            $"/api/travel/trips/{tripId}/expenses?currency={eur}&search=meal&sort=amount&sortDirection=desc&page=1&pageSize=10",
            CancellationToken.None);

        Assert.NotNull(result);
        var expense = Assert.Single(result.Items);
        Assert.Equal("Airport meal", expense.Description);
        Assert.Equal("Meals", expense.ExpenseCategoryName);
        Assert.Equal(ConfigurationCatalog.CurrencyCodes.Euro, expense.CurrencyCode);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Update_and_delete_recompute_trip_currency_totals()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);
        var categoryId = await TravelTestData.ExpenseCategoryIdAsync(server.Services);
        var eur = await TravelTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.Euro);
        var usd = await TravelTestData.CurrencyIdAsync(server.Services, ConfigurationCatalog.CurrencyCodes.UsDollar);
        var expenseId = await SeedExpenseAsync(server.Services, tripId, founderId, categoryId, eur, "Taxi", new DateOnly(2026, 6, 1), 10m);
        await SeedExpenseAsync(server.Services, tripId, founderId, categoryId, eur, "Meal", new DateOnly(2026, 6, 2), 5m);
        var update = new UpdateTravelExpenseRequest(categoryId, "Taxi updated", new DateOnly(2026, 6, 1), 7.50m, usd, null, null, null);

        using var updatedResponse = await CapexApi.PutJsonAsync(client, $"/api/travel/trips/{tripId}/expenses/{expenseId}", update, csrf);
        var tripAfterUpdate = await client.GetFromJsonAsync<TravelTripResponse>($"/api/travel/trips/{tripId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        Assert.Equal(
            new[] { ("EUR", 5m), ("USD", 7.50m) },
            tripAfterUpdate!.ExpenseTotals.Select(total => (total.CurrencyCode, total.Amount)).ToArray());

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/travel/trips/{tripId}/expenses/{expenseId}", csrf);
        var tripAfterDelete = await client.GetFromJsonAsync<TravelTripResponse>($"/api/travel/trips/{tripId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(new[] { ("EUR", 5m) }, tripAfterDelete!.ExpenseTotals.Select(total => (total.CurrencyCode, total.Amount)).ToArray());
    }

    [Fact]
    public async Task Expense_access_follows_parent_trip_visibility()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            visibility: RecordVisibility.Private);
        var expenseId = await SeedExpenseAsync(
            server.Services,
            tripId,
            founderId,
            await TravelTestData.ExpenseCategoryIdAsync(server.Services),
            await TravelTestData.CurrencyIdAsync(server.Services),
            "Private taxi",
            new DateOnly(2026, 6, 1),
            10m);
        await server.CreateUserAsync("travel-expense-member", "TravelExpensePass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "travel-expense-member", "TravelExpensePass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var list = await member.GetAsync($"/api/travel/trips/{tripId}/expenses", CancellationToken.None);
        using var detail = await member.GetAsync($"/api/travel/trips/{tripId}/expenses/{expenseId}", CancellationToken.None);
        using var delete = await CapexApi.DeleteAsync(member, $"/api/travel/trips/{tripId}/expenses/{expenseId}", memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Expense_attachments_round_trip_and_are_cleaned_up_on_expense_delete()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId);
        var expenseId = await SeedExpenseAsync(
            server.Services,
            tripId,
            founderId,
            await TravelTestData.ExpenseCategoryIdAsync(server.Services),
            await TravelTestData.CurrencyIdAsync(server.Services),
            "Taxi",
            new DateOnly(2026, 6, 1),
            10m);
        var content = Encoding.UTF8.GetBytes("Receipt");

        using var upload = await CapexApi.UploadAsync(
            client,
            $"/api/travel/trips/{tripId}/expenses/{expenseId}/attachments",
            "receipt.txt",
            "text/plain",
            content,
            csrf);
        var created = await upload.Content.ReadFromJsonAsync<AttachmentPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("receipt.txt", created!.FileName);

        var detail = await client.GetFromJsonAsync<TravelExpenseResponse>(
            $"/api/travel/trips/{tripId}/expenses/{expenseId}",
            CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(detail!.Attachments).Id);

        var downloaded = await client.GetByteArrayAsync(
            $"/api/travel/trips/{tripId}/expenses/{expenseId}/attachments/{created.Id}",
            CancellationToken.None);
        Assert.Equal(content, downloaded);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/travel/trips/{tripId}/expenses/{expenseId}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        await using var scope = server.Services.CreateAsyncScope();
        var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentService>();
        Assert.Empty(await attachments.ListByOwnerAsync(TravelAttachments.ExpenseOwner(expenseId), CancellationToken.None));
    }

    private static CreateTravelExpenseRequest DefaultCreateRequest(int categoryId, int currencyId) =>
        new(categoryId, "Taxi", new DateOnly(2026, 6, 1), 10m, currencyId, null, null, null);

    private static DateOnly TodayInMadrid()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
    }

    private static async Task<int> SeedExpenseAsync(
        IServiceProvider services,
        int tripId,
        int creatorId,
        int categoryId,
        int currencyId,
        string description,
        DateOnly date,
        decimal amount)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var expense = TravelExpense.Create(
            tripId,
            new TravelExpenseValues(categoryId, description, date, amount, currencyId, null, null, null),
            new(creatorId),
            new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
        database.Add(expense);
        await database.SaveChangesAsync();
        return expense.Id;
    }

    private sealed record AttachmentPayload(string Id, string FileName, string ContentType, long Size);

    private sealed record ProblemPayload(string? Code);
}
