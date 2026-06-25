using System.Diagnostics;
using System.Formats.Tar;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Assets.Mutations;
using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Calendar.Queries;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Api.Modules.Mood.Domain;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Api.Modules.Projects.Mutations;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Api.Persistence;
using Segaris.Api.Platform.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;
using Testcontainers.PostgreSql;
using ProjectActivity = Segaris.Api.Modules.Projects.Domain.Activity;
using SegarisProcess = Segaris.Api.Modules.Processes.Domain.Process;
using SystemProcess = System.Diagnostics.Process;

namespace Segaris.Postgres.IntegrationTests;

public sealed class PostgresPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer? postgres;

    public async Task InitializeAsync()
    {
        try
        {
            postgres = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("segaris_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await postgres.StartAsync();
        }
        catch (DockerUnavailableException) when (!IsContinuousIntegration())
        {
            postgres = null;
        }
    }

    public Task DisposeAsync()
    {
        return postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }

    [Fact]
    public async Task Postgres_supports_foundation_mappings_constraints_and_queries()
    {
        if (postgres is null)
        {
            return;
        }

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var createdAt = new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero);

        database.Set<PersistenceCompatibilityRecord>().Add(new PersistenceCompatibilityRecord
        {
            Name = "Foundation fixture",
            CivilDate = new DateOnly(2026, 6, 12),
            Amount = 1234.56m,
            CurrencyCode = "EUR",
            CreatedAt = createdAt,
        });
        await database.SaveChangesAsync();

        var result = await database.Set<PersistenceCompatibilityRecord>()
            .AsNoTracking()
            .SingleAsync(record => record.Name.Contains("Foundation"));

        Assert.True(result.Id > 0);
        Assert.Equal(new DateOnly(2026, 6, 12), result.CivilDate);
        Assert.Equal(1234.56m, result.Amount);
        Assert.Equal("EUR", result.CurrencyCode);
        Assert.Equal(createdAt, result.CreatedAt);

        database.Set<PersistenceCompatibilityRecord>().Add(new PersistenceCompatibilityRecord
        {
            Name = result.Name,
            CivilDate = result.CivilDate,
            Amount = result.Amount,
            CurrencyCode = result.CurrencyCode,
            CreatedAt = result.CreatedAt,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public async Task Postgres_persists_projects_hierarchy_and_concurrent_number_allocations()
    {
        if (postgres is null)
        {
            return;
        }

        await using var factory = CreateFactory();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var now = new DateTimeOffset(2026, 6, 20, 10, 0, 0, TimeSpan.Zero);
            database.Set<SegarisUser>().Add(new SegarisUser
            {
                Id = 7001,
                UserName = "projects-owner",
                NormalizedUserName = "PROJECTS-OWNER",
                DisplayName = "Projects Owner",
                Language = "en-GB",
                CreatedAt = now,
            });
            await database.SaveChangesAsync();

            var program = ProjectProgram.Create("Infrastructure", "INFR", new UserId(7001), now);
            database.Add(program);
            await database.SaveChangesAsync();
            var axis = ProjectAxis.Create(program.Id, "Websites", "WEBS", new UserId(7001), now);
            database.Add(axis);
            await database.SaveChangesAsync();
        }

        var numbers = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(async index =>
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var allocator = scope.ServiceProvider.GetRequiredService<ProjectNumberAllocator>();
                var number = await allocator.AllocateAsync(CancellationToken.None);
                var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
                var axisId = await database.Set<ProjectAxis>().Select(axis => axis.Id).SingleAsync();
                if (index % 2 == 0)
                {
                    database.Add(Project.Create(
                        new ProjectItemValues(axisId, $"Project {index}", ProjectStatus.Planning, RecordVisibility.Public),
                        number,
                        new UserId(7001),
                        new DateTimeOffset(2026, 6, 20, 10, index, 0, TimeSpan.Zero)));
                }
                else
                {
                    database.Add(ProjectActivity.Create(
                        new ProjectItemValues(axisId, $"Activity {index}", ProjectStatus.Active, RecordVisibility.Public),
                        number,
                        new UserId(7001),
                        new DateTimeOffset(2026, 6, 20, 10, index, 0, TimeSpan.Zero)));
                }

                await database.SaveChangesAsync();
                return number;
            }));

        Assert.Equal(Enumerable.Range(1, 8), numbers.Order());
    }

    [Fact]
    public async Task Postgres_persists_processes_with_steps_and_serves_the_seeded_categories()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-processes-owner";
        const string password = "PgProcessesPass123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));

        int processId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<ProcessCategory>()
                .Where(category => category.Name == "Administrative").Select(category => category.Id).SingleAsync();
            var now = new DateTimeOffset(2026, 6, 21, 10, 0, 0, TimeSpan.Zero);

            var process = SegarisProcess.Create(
                new ProcessValues("Renew passport", categoryId, new DateOnly(2026, 7, 1), null, RecordVisibility.Public),
                new UserId(userId),
                now);
            database.Add(process);
            await database.SaveChangesAsync();
            processId = process.Id;

            database.Add(Step.Create(process.Id, new StepValues("Gather documents", null, null, false), 0, new UserId(userId), now));
            database.Add(Step.Create(process.Id, new StepValues("Optional notarisation", null, null, true), 1, new UserId(userId), now));
            await database.SaveChangesAsync();
        }

        // The Processes catalogue is seeded once with the frozen ordered values.
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();
        var categories = await client.GetFromJsonAsync<JsonElement[]>("/api/processes/categories", CancellationToken.None);
        Assert.Equal(ProcessesDefaults.InitialCategories.Count, categories!.Length);
        Assert.Equal(
            ProcessesDefaults.InitialCategories,
            categories.Select(item => item.GetProperty("name").GetString()));

        // Deleting the process cascades to its owned steps.
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.Equal(2, await verificationDatabase.Set<Step>().CountAsync(step => step.ProcessId == processId));
        var stored = await verificationDatabase.Set<SegarisProcess>().SingleAsync(value => value.Id == processId);
        verificationDatabase.Remove(stored);
        await verificationDatabase.SaveChangesAsync();
        Assert.Equal(0, await verificationDatabase.Set<Step>().CountAsync(step => step.ProcessId == processId));
    }

    [Fact]
    public async Task Postgres_projects_date_bound_source_entries_within_the_calendar_range()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-calendar-owner";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgCalendarPass123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var userId = await database.Set<SegarisUser>()
            .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
        var owner = new UserId(userId);
        var now = new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero);
        var today = new DateOnly(2026, 6, 15);

        var categoryId = await database.Set<InventoryCategory>()
            .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
        var locationId = await database.Set<InventoryLocation>()
            .Where(location => location.Name == "Other").Select(location => location.Id).SingleAsync();
        var supplierId = await database.Set<SegarisSupplier>()
            .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
        var maintenanceTypeId = await database.Set<MaintenanceType>().Select(type => type.Id).FirstAsync();
        var processCategoryId = await database.Set<ProcessCategory>().Select(category => category.Id).FirstAsync();

        var item = InventoryItem.Create(
            new("PG calendar item", InventoryItemStatus.Active, null, categoryId, locationId, 0m, 0m, [supplierId], RecordVisibility.Public),
            owner,
            now);
        database.Add(item);
        await database.SaveChangesAsync();

        // The nullable DateOnly comparisons and the Step/Process join are the
        // provider-sensitive parts of the Calendar projection queries.
        var inRangeOrder = InventoryOrder.Create(
            new(supplierId, InventoryOrderStatus.Planning, currencyId, null, new DateOnly(2026, 6, 20), null, RecordVisibility.Public, [new InventoryOrderLineValues(item.Id, 1m, 5m)]),
            owner,
            now);
        var outOfRangeOrder = InventoryOrder.Create(
            new(supplierId, InventoryOrderStatus.Planning, currencyId, null, new DateOnly(2026, 7, 5), null, RecordVisibility.Public, [new InventoryOrderLineValues(item.Id, 1m, 5m)]),
            owner,
            now);
        database.Add(inRangeOrder);
        database.Add(outOfRangeOrder);

        var task = MaintenanceTask.Create(
            new("PG calendar task", maintenanceTypeId, MaintenanceStatus.InProgress, MaintenancePriority.Medium, new DateOnly(2026, 6, 18), null, null, RecordVisibility.Public),
            owner,
            now,
            today);
        database.Add(task);

        var process = SegarisProcess.Create(
            new ProcessValues("PG calendar process", processCategoryId, null, null, RecordVisibility.Public),
            owner,
            now);
        database.Add(process);
        await database.SaveChangesAsync();

        var inRangeStep = Step.Create(process.Id, new StepValues("Due soon", new DateOnly(2026, 6, 22), null, false), 0, owner, now);
        var outOfRangeStep = Step.Create(process.Id, new StepValues("Later", new DateOnly(2026, 7, 10), null, false), 1, owner, now);
        database.Add(inRangeStep);
        database.Add(outOfRangeStep);
        await database.SaveChangesAsync();

        var reader = scope.ServiceProvider.GetRequiredService<CalendarEntriesReadService>();
        var filter = CalendarEntriesQuery.Create(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));
        var entries = await reader.ListEntriesAsync(filter, owner, CancellationToken.None);
        var ids = entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);

        Assert.Contains($"inventory:order:{inRangeOrder.Id}", ids);
        Assert.Contains($"maintenance:task:{task.Id}", ids);
        Assert.Contains($"processes:step:{inRangeStep.Id}", ids);
        Assert.DoesNotContain($"inventory:order:{outOfRangeOrder.Id}", ids);
        Assert.DoesNotContain($"processes:step:{outOfRangeStep.Id}", ids);
    }

    [Fact]
    public async Task Postgres_supports_the_identity_user_lifecycle()
    {
        if (postgres is null)
        {
            return;
        }

        const string adminUserName = "pg-founder";
        const string adminPassword = "PgFounderPass123!";

        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", adminUserName),
            new("Segaris:Identity:Bootstrap:Password", adminPassword));
        using var admin = factory.CreateClient();

        using var login = await admin.PostAsJsonAsync(
            "/api/session",
            new { userName = adminUserName, password = adminPassword },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();

        using var created = await PostWithCsrfAsync(
            admin,
            "/api/admin/users",
            new { userName = "pg-member", password = "PgMemberPass123!", role = "User" });
        using var duplicate = await PostWithCsrfAsync(
            admin,
            "/api/admin/users",
            new { userName = "pg-member", password = "PgMemberPass123!", role = "User" });

        var list = await admin.GetFromJsonAsync<JsonElement>("/api/admin/users", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(2, list.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Postgres_seeds_and_serves_the_configuration_catalogs()
    {
        if (postgres is null)
        {
            return;
        }

        const string adminUserName = "pg-configuration-admin";
        const string adminPassword = "PgConfigurationPass123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", adminUserName),
            new("Segaris:Identity:Bootstrap:Password", adminPassword));
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName = adminUserName, password = adminPassword },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();

        var suppliers = await client.GetFromJsonAsync<JsonElement[]>(
            "/api/configuration/suppliers",
            CancellationToken.None);
        var costCenters = await client.GetFromJsonAsync<JsonElement[]>(
            "/api/configuration/cost-centers",
            CancellationToken.None);
        var currencies = await client.GetFromJsonAsync<JsonElement[]>(
            "/api/configuration/currencies",
            CancellationToken.None);

        Assert.Equal(6, suppliers!.Length);
        Assert.Equal(5, costCenters!.Length);
        Assert.Equal(3, currencies!.Length);
        Assert.Contains(currencies, item => item.GetProperty("code").GetString() == "EUR");
    }

    [Fact]
    public async Task Postgres_persists_capex_decimals_order_and_rounded_total()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-capex-admin";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgCapexPass123!"));
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var userId = await database.Set<SegarisUser>()
            .Where(user => user.UserName == userName)
            .Select(user => user.Id)
            .SingleAsync();
        var categoryId = await database.Set<CapexCategory>()
            .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
        var now = new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);
        var entry = CapexEntry.Create(
            new("Postgres entry", CapexMovementType.Expense, CapexEntryStatus.Planning,
                new DateOnly(2026, 6, 14), categoryId, null, null, currencyId, null, RecordVisibility.Public),
            [new("Rounded", 0.01m, 0.50m), new("Normal", 3m, 2.25m)],
            new UserId(userId), now);
        database.Add(entry);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var stored = await database.Set<CapexEntry>().Include(value => value.Items)
            .SingleAsync(value => value.Title == "Postgres entry");

        Assert.Equal(6.76m, stored.TotalAmount);
        Assert.Equal([0, 1], stored.Items.OrderBy(item => item.Position).Select(item => item.Position));
        Assert.Equal([0.01m, 6.75m], stored.Items.OrderBy(item => item.Position).Select(item => item.LineAmount));
    }

    [Fact]
    public async Task Postgres_serves_capex_and_opex_financial_projections_with_date_bounds_and_decimals()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-analytics-source";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgAnalyticsSource123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var userId = await database.Set<SegarisUser>().Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
        var capexCategoryId = await database.Set<CapexCategory>()
            .Where(category => category.Name == "Home").Select(category => category.Id).SingleAsync();
        var opexCategoryId = await database.Set<OpexCategory>()
            .Where(category => category.Name == "Housing").Select(category => category.Id).SingleAsync();
        var supplierId = await database.Set<SegarisSupplier>()
            .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
        var costCenterId = await database.Set<SegarisCostCenter>()
            .Where(costCenter => costCenter.Name == "Household").Select(costCenter => costCenter.Id).SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
        var actor = new UserId(userId);
        var now = new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);

        var capexEntry = CapexEntry.Create(
            new("Projected Capex", CapexMovementType.Expense, CapexEntryStatus.Completed,
                new DateOnly(2026, 1, 1), capexCategoryId, supplierId, costCenterId, currencyId, null, RecordVisibility.Public),
            [new("Rounded", 0.01m, 0.50m), new("Normal", 3m, 2.25m)],
            actor,
            now);
        database.Add(capexEntry);
        database.Add(CapexEntry.Create(
            new("Excluded planning", CapexMovementType.Expense, CapexEntryStatus.Planning,
                new DateOnly(2026, 6, 14), capexCategoryId, supplierId, costCenterId, currencyId, null, RecordVisibility.Public),
            [new("Nope", 1m, 99m)],
            actor,
            now));

        var opexContract = OpexContract.Create(
            new("Projected Opex", OpexMovementType.Income, OpexContractStatus.Active,
                null, null, null, OpexExpectedFrequency.Monthly,
                opexCategoryId, supplierId, costCenterId, currencyId, null, RecordVisibility.Public),
            actor,
            now);
        database.Add(opexContract);
        await database.SaveChangesAsync();
        database.Add(OpexOccurrence.Create(
            opexContract.Id,
            new(new DateOnly(2026, 12, 31), 123.45m, null, null),
            actor,
            now));
        database.Add(OpexOccurrence.Create(
            opexContract.Id,
            new(new DateOnly(2027, 1, 1), 999m, null, null),
            actor,
            now));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var capexProvider = scope.ServiceProvider.GetRequiredService<ICapexFinancialProjectionProvider>();
        var opexProvider = scope.ServiceProvider.GetRequiredService<IOpexFinancialProjectionProvider>();
        var capex = await capexProvider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            actor,
            CancellationToken.None);
        var opex = await opexProvider.ListFinancialProjectionsAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            actor,
            CancellationToken.None);

        var capexProjection = Assert.Single(capex);
        Assert.Equal($"capex:{capexEntry.Id}", capexProjection.SourceId);
        Assert.Equal(new DateOnly(2026, 1, 1), capexProjection.AccountingDate);
        Assert.Equal(6.76m, capexProjection.Amount);
        Assert.Equal("Expense", capexProjection.MovementDirection);
        Assert.Equal(
            ("EUR", "Home", "Amazon", "Household"),
            (capexProjection.CurrencyCode, capexProjection.CategoryLabel, capexProjection.SupplierLabel, capexProjection.CostCenterLabel));

        var opexProjection = Assert.Single(opex);
        Assert.Equal(new DateOnly(2026, 12, 31), opexProjection.AccountingDate);
        Assert.Equal(123.45m, opexProjection.Amount);
        Assert.Equal("Income", opexProjection.MovementDirection);
        Assert.Equal(
            ("EUR", "Housing", "Amazon", "Household"),
            (opexProjection.CurrencyCode, opexProjection.CategoryLabel, opexProjection.SupplierLabel, opexProjection.CostCenterLabel));
    }

    [Fact]
    public async Task Postgres_receives_inventory_orders_atomically_and_preserves_decimals()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-inventory-receive";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgInventoryReceive123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<InventoryOrderWriteService>();
        var userId = await database.Set<SegarisUser>().Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
        var categoryId = await database.Set<InventoryCategory>().Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
        var locationId = await database.Set<InventoryLocation>().Where(location => location.Name == "Other").Select(location => location.Id).SingleAsync();
        var supplierId = await database.Set<SegarisSupplier>().Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
        var now = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var item = InventoryItem.Create(
            new("Postgres olive oil", InventoryItemStatus.Active, null, categoryId, locationId, 1.10m, 0m, [supplierId], RecordVisibility.Public),
            new UserId(userId),
            now);
        database.Add(item);
        await database.SaveChangesAsync();
        var order = InventoryOrder.Create(
            new(supplierId, InventoryOrderStatus.Active, currencyId, new DateOnly(2026, 6, 16), null, null, RecordVisibility.Public,
                [new InventoryOrderLineValues(item.Id, 2.25m, 12.34m)]),
            new UserId(userId),
            now);
        database.Add(order);
        await database.SaveChangesAsync();

        Assert.True(await service.ReceiveAsync(order.Id, new UserId(userId), CancellationToken.None));

        database.ChangeTracker.Clear();
        var storedItem = await database.Set<InventoryItem>().SingleAsync(value => value.Id == item.Id);
        var storedOrder = await database.Set<InventoryOrder>().SingleAsync(value => value.Id == order.Id);
        Assert.Equal(3.35m, storedItem.CurrentStock);
        Assert.Equal(InventoryOrderStatus.Received, storedOrder.Status);
    }

    [Fact]
    public async Task Postgres_persists_mood_entries_and_preserves_owner_date_order()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-mood-owner";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgMoodPass123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var userId = await database.Set<SegarisUser>().Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
        var now = new DateTimeOffset(2026, 6, 18, 10, 0, 0, TimeSpan.Zero);
        database.Add(MoodEntry.Create(
            new(new DateOnly(2026, 6, 18), 4, MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.Internal, "first"),
            new UserId(userId),
            now));
        database.Add(MoodEntry.Create(
            new(new DateOnly(2026, 6, 18), 2, MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.External, "second"),
            new UserId(userId),
            now.AddMinutes(1)));
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var stored = await database.Set<MoodEntry>()
            .Where(entry => entry.CreatedBy == userId && entry.EntryDate == new DateOnly(2026, 6, 18))
            .OrderBy(entry => entry.EntryDate)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        Assert.Equal(["first", "second"], stored.Select(entry => entry.Notes));
        Assert.Equal("Burnout", MoodDerivedEmotionMatrix.Resolve(
            stored[1].Energy,
            stored[1].Alignment,
            stored[1].Direction,
            stored[1].Source));
    }

    [Fact]
    public async Task Postgres_aggregates_the_mood_dashboard_for_the_current_user()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-mood-dashboard";
        const string password = "PgMoodDashboard123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var now = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            database.Add(MoodEntry.Create(
                new(new DateOnly(2026, 1, 5), 2, MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.Internal, null),
                new UserId(userId), now));
            database.Add(MoodEntry.Create(
                new(new DateOnly(2026, 1, 5), 4, MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.External, null),
                new UserId(userId), now.AddMinutes(1)));
            database.Add(MoodEntry.Create(
                new(new DateOnly(2026, 3, 10), 5, MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.Internal, null),
                new UserId(userId), now));
            // Outside the selected year, so it must not contribute.
            database.Add(MoodEntry.Create(
                new(new DateOnly(2025, 12, 31), 1, MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.External, null),
                new UserId(userId), now));
            await database.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();

        var dashboard = await client.GetFromJsonAsync<MoodDashboardResponse>(
            "/api/mood/dashboard?scale=year&period=2026",
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal(3, dashboard.EntryCount);

        // Score by day of week: Monday holds the two January entries, Tuesday the March one.
        var monday = dashboard.ScoreByDayOfWeek.Single(day => day.DayOfWeek == "Monday");
        Assert.Equal(2, monday.MinScore);
        Assert.Equal(3.0d, monday.AverageScore);
        Assert.Equal(4, monday.MaxScore);
        Assert.Equal(5.0d, dashboard.ScoreByDayOfWeek.Single(day => day.DayOfWeek == "Tuesday").AverageScore);

        // Month buckets and arithmetic average evaluated end-to-end against PostgreSQL.
        var january = dashboard.Buckets.Single(bucket => bucket.Key == "2026-01");
        Assert.Equal(3.0d, january.AverageScore);
        Assert.Equal(5.0d, dashboard.Buckets.Single(bucket => bucket.Key == "2026-03").AverageScore);

        // Criteria distribution counts every enum value across the three in-year entries.
        Assert.Equal(1, dashboard.Distribution.Energy.Single(value => value.Value == "Low").Count);
        Assert.Equal(1, dashboard.Distribution.Energy.Single(value => value.Value == "Medium").Count);
        Assert.Equal(1, dashboard.Distribution.Energy.Single(value => value.Value == "High").Count);
        Assert.Equal(2, dashboard.Distribution.Source.Single(value => value.Value == "Internal").Count);
        Assert.Equal(1, dashboard.Distribution.Source.Single(value => value.Value == "External").Count);
    }

    [Fact]
    public async Task Postgres_rolls_back_inventory_receipt_when_one_stock_update_fails()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-inventory-receive-rollback";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgInventoryRollback123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<InventoryOrderWriteService>();
        var userId = await database.Set<SegarisUser>().Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
        var categoryId = await database.Set<InventoryCategory>().Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
        var locationId = await database.Set<InventoryLocation>().Where(location => location.Name == "Other").Select(location => location.Id).SingleAsync();
        var supplierId = await database.Set<SegarisSupplier>().Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
        var currencyId = await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
        var now = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var firstItem = InventoryItem.Create(
            new("Postgres flour", InventoryItemStatus.Active, null, categoryId, locationId, 4m, 0m, [supplierId], RecordVisibility.Public),
            new UserId(userId),
            now);
        var overflowingItem = InventoryItem.Create(
            new("Postgres rice", InventoryItemStatus.Active, null, categoryId, locationId, 9999999999999999.99m, 0m, [supplierId], RecordVisibility.Public),
            new UserId(userId),
            now);
        database.AddRange(firstItem, overflowingItem);
        await database.SaveChangesAsync();
        var order = InventoryOrder.Create(
            new(supplierId, InventoryOrderStatus.Active, currencyId, new DateOnly(2026, 6, 16), null, null, RecordVisibility.Public,
                [
                    new InventoryOrderLineValues(firstItem.Id, 2m, 4m),
                    new InventoryOrderLineValues(overflowingItem.Id, 0.01m, 1m),
                ]),
            new UserId(userId),
            now);
        database.Add(order);
        await database.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.ReceiveAsync(order.Id, new UserId(userId), CancellationToken.None));

        database.ChangeTracker.Clear();
        Assert.Equal(4m, await database.Set<InventoryItem>().Where(item => item.Id == firstItem.Id).Select(item => item.CurrentStock).SingleAsync());
        Assert.Equal(
            9999999999999999.99m,
            await database.Set<InventoryItem>().Where(item => item.Id == overflowingItem.Id).Select(item => item.CurrentStock).SingleAsync());
        Assert.Equal(
            InventoryOrderStatus.Active,
            await database.Set<InventoryOrder>().Where(value => value.Id == order.Id).Select(value => value.Status).SingleAsync());
    }

    [Fact]
    public async Task Postgres_replaces_supplier_references_and_deletes_the_source_atomically()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-configuration-migration";
        const string password = "PgConfigurationMigration123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        int entryId;
        int sourceId;
        int replacementId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<CapexCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var currencyId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
                .Select(currency => currency.Id).SingleAsync();
            var nextSortOrder = (await database.Set<SegarisSupplier>()
                .Select(value => (int?)value.SortOrder).MaxAsync() ?? -1) + 1;
            var catalogNow = new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);
            var source = new SegarisSupplier
            {
                Name = "Postgres migration source",
                NormalizedName = CatalogNormalization.Normalize("Postgres migration source"),
                SortOrder = nextSortOrder,
                CreatedAt = catalogNow,
                CreatedBy = userId,
                UpdatedAt = catalogNow,
                UpdatedBy = userId,
            };
            var replacement = new SegarisSupplier
            {
                Name = "Postgres migration target",
                NormalizedName = CatalogNormalization.Normalize("Postgres migration target"),
                SortOrder = nextSortOrder + 1,
                CreatedAt = catalogNow,
                CreatedBy = userId,
                UpdatedAt = catalogNow,
                UpdatedBy = userId,
            };
            database.AddRange(source, replacement);
            await database.SaveChangesAsync();
            sourceId = source.Id;
            replacementId = replacement.Id;
            var entry = CapexEntry.Create(
                new("Postgres migration", CapexMovementType.Expense, CapexEntryStatus.Planning,
                    new DateOnly(2026, 6, 14), categoryId, sourceId, null, currencyId, null, RecordVisibility.Private),
                [new("Migrated", 1m, 1m)],
                new UserId(userId),
                new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero));
            database.Add(entry);
            await database.SaveChangesAsync();
            entryId = entry.Id;
        }

        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();
        using var migrated = await PostWithCsrfAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new { replacementId, clearReferences = false, exchangeRate = (decimal?)null });

        Assert.Equal(HttpStatusCode.NoContent, migrated.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var stored = await verificationDatabase.Set<CapexEntry>().SingleAsync(value => value.Id == entryId);
        Assert.Equal(replacementId, stored.SupplierId);
        Assert.False(await verificationDatabase.Set<SegarisSupplier>().AnyAsync(value => value.Id == sourceId));
    }

    [Fact]
    public async Task Postgres_converts_currency_recalculates_decimals_and_deletes_the_source_atomically()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-currency-conversion";
        const string password = "PgCurrencyConversion123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        int entryId;
        int sourceId;
        int targetId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<CapexCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            sourceId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == "EUR").Select(currency => currency.Id).SingleAsync();
            targetId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == "USD").Select(currency => currency.Id).SingleAsync();
            var entry = CapexEntry.Create(
                new("Postgres conversion", CapexMovementType.Expense, CapexEntryStatus.Planning,
                    new DateOnly(2026, 6, 14), categoryId, null, null, sourceId, null, RecordVisibility.Private),
                [new("Desks", 2m, 10.00m), new("Lamp", 1m, 5.55m)],
                new UserId(userId),
                new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero));
            database.Add(entry);
            await database.SaveChangesAsync();
            entryId = entry.Id;
        }

        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();
        using var converted = await PostWithCsrfAsync(
            client,
            $"/api/configuration/currencies/{sourceId}/replace-and-delete",
            new { replacementId = targetId, clearReferences = false, exchangeRate = (decimal?)1.20m });

        Assert.Equal(HttpStatusCode.NoContent, converted.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var stored = await verificationDatabase.Set<CapexEntry>().Include(value => value.Items)
            .SingleAsync(value => value.Id == entryId);
        Assert.Equal(targetId, stored.CurrencyId);
        Assert.Equal([12.00m, 6.66m], stored.Items.OrderBy(item => item.Position).Select(item => item.UnitAmount));
        Assert.Equal([24.00m, 6.66m], stored.Items.OrderBy(item => item.Position).Select(item => item.LineAmount));
        Assert.Equal(30.66m, stored.TotalAmount);
        Assert.False(await verificationDatabase.Set<SegarisCurrency>().AnyAsync(value => value.Id == sourceId));
    }

    [Fact]
    public async Task Postgres_replaces_inventory_supplier_references_and_deletes_the_source_atomically()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-inventory-supplier-migration";
        const string password = "PgInventorySupplier123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        int itemId;
        int orderId;
        int sourceId;
        int replacementId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<InventoryCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var locationId = await database.Set<InventoryLocation>()
                .Where(location => location.Name == "Other").Select(location => location.Id).SingleAsync();
            var currencyId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default)
                .Select(currency => currency.Id).SingleAsync();
            var nextSortOrder = (await database.Set<SegarisSupplier>()
                .Select(value => (int?)value.SortOrder).MaxAsync() ?? -1) + 1;
            var catalogNow = new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);
            var source = new SegarisSupplier
            {
                Name = "PG inventory source",
                NormalizedName = CatalogNormalization.Normalize("PG inventory source"),
                SortOrder = nextSortOrder,
                CreatedAt = catalogNow,
                CreatedBy = userId,
                UpdatedAt = catalogNow,
                UpdatedBy = userId,
            };
            var replacement = new SegarisSupplier
            {
                Name = "PG inventory target",
                NormalizedName = CatalogNormalization.Normalize("PG inventory target"),
                SortOrder = nextSortOrder + 1,
                CreatedAt = catalogNow,
                CreatedBy = userId,
                UpdatedAt = catalogNow,
                UpdatedBy = userId,
            };
            database.AddRange(source, replacement);
            await database.SaveChangesAsync();
            sourceId = source.Id;
            replacementId = replacement.Id;

            var item = InventoryItem.Create(
                new("PG migration oil", InventoryItemStatus.Active, null, categoryId, locationId, 1m, 0m, [sourceId], RecordVisibility.Private),
                new UserId(userId),
                catalogNow);
            database.Add(item);
            await database.SaveChangesAsync();
            itemId = item.Id;

            var order = InventoryOrder.Create(
                new(sourceId, InventoryOrderStatus.Active, currencyId, new DateOnly(2026, 6, 16), null, null, RecordVisibility.Private,
                    [new InventoryOrderLineValues(item.Id, 1m, 10.00m)]),
                new UserId(userId),
                catalogNow);
            database.Add(order);
            await database.SaveChangesAsync();
            orderId = order.Id;
        }

        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();
        using var migrated = await PostWithCsrfAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new { replacementId, clearReferences = false, exchangeRate = (decimal?)null });

        Assert.Equal(HttpStatusCode.NoContent, migrated.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var storedOrder = await verificationDatabase.Set<InventoryOrder>().SingleAsync(value => value.Id == orderId);
        var eligibility = await verificationDatabase.Set<InventoryItemSupplier>()
            .Where(association => association.ItemId == itemId)
            .Select(association => association.SupplierId)
            .ToListAsync();
        Assert.Equal(replacementId, storedOrder.SupplierId);
        Assert.Equal([replacementId], eligibility);
        Assert.False(await verificationDatabase.Set<SegarisSupplier>().AnyAsync(value => value.Id == sourceId));
    }

    [Fact]
    public async Task Postgres_reassigns_maintenance_asset_references_and_deletes_the_source_atomically()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-maintenance-asset-admin";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", "PgMaintenanceAssetPass123!"));
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var write = scope.ServiceProvider.GetRequiredService<AssetWriteService>();
        var actor = new UserId(await database.Set<SegarisUser>()
            .Where(user => user.UserName == userName)
            .Select(user => user.Id)
            .SingleAsync());
        var now = new DateTimeOffset(2026, 6, 19, 11, 0, 0, TimeSpan.Zero);
        var categoryId = await database.Set<AssetCategory>()
            .Where(category => category.Name == "Other")
            .Select(category => category.Id)
            .SingleAsync();
        var locationId = await database.Set<AssetLocation>()
            .Where(location => location.Name == "Other")
            .Select(location => location.Id)
            .SingleAsync();
        var typeId = await database.Set<MaintenanceType>()
            .Where(type => type.Name == "Repair")
            .Select(type => type.Id)
            .SingleAsync();
        var source = Asset.Create(
            new("Postgres source asset", categoryId, locationId, AssetStatus.Active, null, null, null, null, null, null, RecordVisibility.Public),
            actor,
            now);
        var target = Asset.Create(
            new("Postgres target asset", categoryId, locationId, AssetStatus.Active, null, null, null, null, null, null, RecordVisibility.Public),
            actor,
            now);
        database.AddRange(source, target);
        await database.SaveChangesAsync();

        var task = MaintenanceTask.Create(
            new("Postgres referenced task", typeId, MaintenanceStatus.Pending, MaintenancePriority.Medium, null, null, source.Id, RecordVisibility.Public),
            actor,
            now,
            new DateOnly(2026, 6, 19));
        database.Add(task);
        await database.SaveChangesAsync();

        var outcome = await write.ReassignAndDeleteAsync(
            source.Id,
            new AssetReassignmentDeletionRequest(target.Id),
            actor,
            CancellationToken.None);

        Assert.Equal(AssetDeletionOutcome.Deleted, outcome);
        Assert.False(await database.Set<Asset>().AnyAsync(asset => asset.Id == source.Id));
        Assert.Equal(target.Id, await database.Set<MaintenanceTask>()
            .Where(candidate => candidate.Id == task.Id)
            .Select(candidate => candidate.AssetId)
            .SingleAsync());
    }

    [Fact]
    public async Task Postgres_converts_inventory_order_currency_and_deletes_the_source_atomically()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-inventory-currency-migration";
        const string password = "PgInventoryCurrency123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        int orderId;
        int sourceId;
        int targetId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<InventoryCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var locationId = await database.Set<InventoryLocation>()
                .Where(location => location.Name == "Other").Select(location => location.Id).SingleAsync();
            var supplierId = await database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
            sourceId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == "EUR").Select(currency => currency.Id).SingleAsync();
            targetId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == "USD").Select(currency => currency.Id).SingleAsync();
            var catalogNow = new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);
            var item = InventoryItem.Create(
                new("PG conversion oil", InventoryItemStatus.Active, null, categoryId, locationId, 1m, 0m, [supplierId], RecordVisibility.Private),
                new UserId(userId),
                catalogNow);
            database.Add(item);
            await database.SaveChangesAsync();
            var order = InventoryOrder.Create(
                new(supplierId, InventoryOrderStatus.Active, sourceId, new DateOnly(2026, 6, 16), null, null, RecordVisibility.Private,
                    [new InventoryOrderLineValues(item.Id, 2m, 10.00m), new InventoryOrderLineValues(item.Id, 1m, 5.55m)]),
                new UserId(userId),
                catalogNow);
            database.Add(order);
            await database.SaveChangesAsync();
            orderId = order.Id;
        }

        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();
        using var converted = await PostWithCsrfAsync(
            client,
            $"/api/configuration/currencies/{sourceId}/replace-and-delete",
            new { replacementId = targetId, clearReferences = false, exchangeRate = (decimal?)1.20m });

        Assert.Equal(HttpStatusCode.NoContent, converted.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var storedOrder = await verificationDatabase.Set<InventoryOrder>()
            .Include(value => value.Lines)
            .SingleAsync(value => value.Id == orderId);
        Assert.Equal(targetId, storedOrder.CurrencyId);
        // 10.00 * 1.20 = 12.00; 5.55 * 1.20 = 6.66.
        Assert.Equal([12.00m, 6.66m], storedOrder.Lines.OrderBy(line => line.Id).Select(line => line.LineTotal));
        Assert.False(await verificationDatabase.Set<SegarisCurrency>().AnyAsync(value => value.Id == sourceId));
    }

    [Fact]
    public async Task Postgres_clears_recipe_ingredient_item_references_and_deletes_inventory_item_atomically()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-inventory-recipe-deletion";
        const string password = "PgInventoryRecipeDeletion123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        int itemId;
        int recipeId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var write = scope.ServiceProvider.GetRequiredService<InventoryItemWriteService>();
            var userId = await database.Set<SegarisUser>()
                .Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var actor = new UserId(userId);
            var categoryId = await database.Set<InventoryCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var locationId = await database.Set<InventoryLocation>()
                .Where(location => location.Name == "Other").Select(location => location.Id).SingleAsync();
            var supplierId = await database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
            var recipeCategoryId = await database.Set<RecipeCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var now = new DateTimeOffset(2026, 6, 22, 10, 0, 0, TimeSpan.Zero);
            var item = InventoryItem.Create(
                new("PG recipe flour", InventoryItemStatus.Active, null, categoryId, locationId, 1m, 0m, [supplierId], RecordVisibility.Public),
                actor,
                now);
            database.Add(item);
            await database.SaveChangesAsync();
            itemId = item.Id;
            var recipe = Recipe.Create(
                new("PG linked recipe", recipeCategoryId, null, null, null, null, [new RecipeIngredientValues("Flour", "500g", itemId)], [], null, RecordVisibility.Public),
                actor,
                now);
            database.Add(recipe);
            await database.SaveChangesAsync();
            recipeId = recipe.Id;

            Assert.True(await write.DeleteAsync(itemId, actor, CancellationToken.None));
        }

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await verificationDatabase.Set<InventoryItem>().AnyAsync(item => item.Id == itemId));
        Assert.Null(await verificationDatabase.Set<RecipeIngredient>()
            .Where(ingredient => ingredient.RecipeId == recipeId)
            .Select(ingredient => ingredient.ItemId)
            .SingleAsync());
    }

    [Fact]
    public async Task Postgres_replaces_capex_items_and_cascades_entry_deletion_through_the_api()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-capex-writer";
        const string password = "PgCapexWritePass123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();
        var token = await client.GetFromJsonAsync<JsonElement>("/api/session/antiforgery", CancellationToken.None);
        var csrf = token.GetProperty("csrfToken").GetString()!;

        int categoryId;
        int currencyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            categoryId = await database.Set<CapexCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            currencyId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
        }

        var createBody = new
        {
            title = "PG original",
            movementType = "Expense",
            status = "Planning",
            dueDate = "2026-06-14",
            categoryId,
            supplierId = (int?)null,
            costCenterId = (int?)null,
            currencyId,
            notes = (string?)null,
            visibility = "Public",
            items = new[] { new { description = "Old line", quantity = 1m, unitAmount = 10m } },
        };
        var entryId = (await SendJsonAsync(client, HttpMethod.Post, "/api/capex/entries", createBody, csrf))
            .GetProperty("id").GetInt32();

        // Replacing the ordered collection reuses positions 0 and 1, which exercises
        // the unique (EntryId, Position) index under PostgreSQL's per-statement
        // constraint checking during a single transactional save.
        var updateBody = new
        {
            title = "PG revised",
            movementType = "Income",
            status = "Completed",
            dueDate = "2026-06-14",
            categoryId,
            supplierId = (int?)null,
            costCenterId = (int?)null,
            currencyId,
            notes = (string?)null,
            visibility = "Public",
            items = new[]
            {
                new { description = "Second", quantity = 1m, unitAmount = 5m },
                new { description = "First", quantity = 2m, unitAmount = 10m },
            },
        };
        var updated = await SendJsonAsync(client, HttpMethod.Put, $"/api/capex/entries/{entryId}", updateBody, csrf);
        Assert.Equal(25.00m, updated.GetProperty("totalAmount").GetDecimal());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var stored = await database.Set<CapexEntry>().AsNoTracking().Include(value => value.Items)
                .SingleAsync(value => value.Id == entryId);
            Assert.Equal(
                ["Second", "First"],
                stored.Items.OrderBy(item => item.Position).Select(item => item.Description));
            Assert.Equal([0, 1], stored.Items.OrderBy(item => item.Position).Select(item => item.Position));
        }

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/capex/entries/{entryId}");
        delete.Headers.Add("X-CSRF-TOKEN", csrf);
        using var deleteResponse = await client.SendAsync(delete, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            Assert.False(await database.Set<CapexEntry>().AnyAsync(entry => entry.Id == entryId));
            // The database-level cascade removes the dependent items.
            Assert.Equal(0, await database.Set<CapexItem>().CountAsync(item => item.EntryId == entryId));
        }
    }

    private static async Task<JsonElement> SendJsonAsync<T>(
        HttpClient client,
        HttpMethod method,
        string route,
        T body,
        string csrf)
    {
        using var request = new HttpRequestMessage(method, route)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request, CancellationToken.None);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
    }

    [Fact]
    public async Task Postgres_serves_searched_filtered_and_attention_capex_reads()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-capex-reader";
        const string password = "PgCapexReadPass123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();

        var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, madrid).Date);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>().Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<CapexCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var currencyId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
            var supplierId = await database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
            var now = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

            // A planning entry due today, matched by an upper-case search term to
            // prove the production case-insensitive search, with a supplier.
            database.Add(CapexEntry.Create(
                new("WIDGET purchase", CapexMovementType.Expense, CapexEntryStatus.Planning,
                    today, categoryId, supplierId, null, currencyId, null, RecordVisibility.Public),
                [new("A widget", 1m, 10m)],
                new UserId(userId), now));
            // Matched only through an item description, with no supplier.
            database.Add(CapexEntry.Create(
                new("Office order", CapexMovementType.Expense, CapexEntryStatus.Completed,
                    today.AddDays(-2), categoryId, null, null, currencyId, null, RecordVisibility.Public),
                [new("Spare widget", 1m, 5m)],
                new UserId(userId), now));
            // Unrelated and not overdue.
            database.Add(CapexEntry.Create(
                new("Stationery", CapexMovementType.Expense, CapexEntryStatus.Planning,
                    today.AddDays(5), categoryId, null, null, currencyId, null, RecordVisibility.Public),
                [new("Pens", 1m, 1m)],
                new UserId(userId), now));
            await database.SaveChangesAsync();
        }

        var searched = await client.GetFromJsonAsync<JsonElement>(
            "/api/capex/entries?search=widget", CancellationToken.None);
        var sortedBySupplier = await client.GetFromJsonAsync<JsonElement>(
            "/api/capex/entries?sort=supplier&sortDirection=asc", CancellationToken.None);
        var attention = await client.GetFromJsonAsync<JsonElement>(
            "/api/launcher/attention", CancellationToken.None);

        // Case-insensitive partial search across title and item descriptions.
        Assert.Equal(2, searched.GetProperty("totalCount").GetInt32());
        // Supplier ascending places the only supplier-bearing entry first and nulls last.
        Assert.Equal(
            "WIDGET purchase",
            sortedBySupplier.GetProperty("items")[0].GetProperty("title").GetString());
        // A planning entry due today activates the launcher attention.
        var capex = attention.GetProperty("modules").EnumerateArray()
            .Single(module => module.GetProperty("module").GetString() == "capex");
        Assert.True(capex.GetProperty("requiresAttention").GetBoolean());
    }

    [Fact]
    public async Task Postgres_serves_searched_filtered_and_aggregated_opex_reads()
    {
        if (postgres is null)
        {
            return;
        }

        const string userName = "pg-opex-reader";
        const string password = "PgOpexReadPass123!";
        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", userName),
            new("Segaris:Identity:Bootstrap:Password", password));
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();

        var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var year = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, madrid).Year;
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
            var userId = await database.Set<SegarisUser>().Where(user => user.UserName == userName).Select(user => user.Id).SingleAsync();
            var categoryId = await database.Set<OpexCategory>()
                .Where(category => category.Name == "Other").Select(category => category.Id).SingleAsync();
            var currencyId = await database.Set<SegarisCurrency>()
                .Where(currency => currency.Code == ConfigurationCatalog.CurrencyCodes.Default).Select(currency => currency.Id).SingleAsync();
            var supplierId = await database.Set<SegarisSupplier>()
                .Where(supplier => supplier.Name == "Amazon").Select(supplier => supplier.Id).SingleAsync();
            var now = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

            // Matched by an upper-case search term against the lower-case name, with a
            // supplier and current-year occurrences spanning the natural-year boundary.
            var widget = OpexContract.Create(
                new("WIDGET subscription", OpexMovementType.Expense, OpexContractStatus.Active,
                    null, null, null, OpexExpectedFrequency.Monthly, categoryId, supplierId, null, currencyId, null, RecordVisibility.Public),
                new UserId(userId), now);
            database.Add(widget);
            await database.SaveChangesAsync();
            database.Add(OpexOccurrence.Create(widget.Id, new(yearStart, 100.50m, null, null), new UserId(userId), now));
            database.Add(OpexOccurrence.Create(widget.Id, new(yearEnd, 49.50m, null, null), new UserId(userId), now));
            database.Add(OpexOccurrence.Create(widget.Id, new(yearStart.AddDays(-1), 1000m, null, null), new UserId(userId), now));
            database.Add(OpexOccurrence.Create(widget.Id, new(yearEnd.AddDays(1), 2000m, null, null), new UserId(userId), now));

            // Matched only through the notes, with no supplier and no occurrences.
            var memo = OpexContract.Create(
                new("Office lease", OpexMovementType.Expense, OpexContractStatus.Active,
                    null, null, null, OpexExpectedFrequency.Annual, categoryId, null, null, currencyId, "Includes a widget clause", RecordVisibility.Public),
                new UserId(userId), now);
            database.Add(memo);

            // Unrelated contract that must not match the search.
            var unrelated = OpexContract.Create(
                new("Cleaning service", OpexMovementType.Expense, OpexContractStatus.Active,
                    null, null, null, OpexExpectedFrequency.Monthly, categoryId, null, null, currencyId, null, RecordVisibility.Public),
                new UserId(userId), now);
            database.Add(unrelated);
            await database.SaveChangesAsync();
        }

        var searched = await client.GetFromJsonAsync<JsonElement>(
            "/api/opex/contracts?search=widget", CancellationToken.None);
        var sortedBySupplier = await client.GetFromJsonAsync<JsonElement>(
            "/api/opex/contracts?sort=supplier&sortDirection=asc", CancellationToken.None);

        // Case-insensitive partial search across the contract name and notes.
        Assert.Equal(2, searched.GetProperty("totalCount").GetInt32());
        // Supplier ascending places the only supplier-bearing contract first and nulls last.
        Assert.Equal(
            "WIDGET subscription",
            sortedBySupplier.GetProperty("items")[0].GetProperty("name").GetString());
        // The current-year realized amount sums only the two in-year occurrences, with the
        // decimal SUM and the natural-year boundary evaluated entirely by PostgreSQL.
        var widgetSummary = searched.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "WIDGET subscription");
        Assert.Equal(150.00m, widgetSummary.GetProperty("realizedCurrentYearAmount").GetDecimal());

        // A contract without qualifying occurrences reports a preserved zero.
        var memoSummary = searched.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Office lease");
        Assert.Equal(0m, memoSummary.GetProperty("realizedCurrentYearAmount").GetDecimal());
    }

    [Fact]
    public async Task Postgres_supports_attachment_metadata_lifecycle()
    {
        if (postgres is null)
        {
            return;
        }

        const string adminUserName = "pg-attachment-admin";
        const string adminPassword = "PgAttachmentPass123!";
        var attachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-pg-attachments-{Guid.NewGuid():N}");
        try
        {
            await using var factory = CreateFactory(
                new("Segaris:Storage:AttachmentsPath", attachmentsPath),
                new("Segaris:Identity:Bootstrap:UserName", adminUserName),
                new("Segaris:Identity:Bootstrap:Password", adminPassword));
            using var client = factory.CreateClient();
            using var login = await client.PostAsJsonAsync(
                "/api/session",
                new { userName = adminUserName, password = adminPassword },
                CancellationToken.None);
            login.EnsureSuccessStatusCode();

            var token = await client.GetFromJsonAsync<JsonElement>(
                "/api/session/antiforgery",
                CancellationToken.None);
            using var multipart = new MultipartFormDataContent();
            var file = new ByteArrayContent("postgres attachment"u8.ToArray());
            file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipart.Add(file, "file", "postgres.txt");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/attachments")
            {
                Content = multipart,
            };
            request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("csrfToken").GetString());
            using var created = await client.SendAsync(request, CancellationToken.None);
            var descriptor = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
            var id = descriptor.GetProperty("id").GetProperty("value").GetInt32();

            var metadata = await client.GetFromJsonAsync<JsonElement>(
                $"/api/platform/attachments/{id}/metadata",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal("postgres.txt", metadata.GetProperty("fileName").GetString());
        }
        finally
        {
            try
            {
                Directory.Delete(attachmentsPath, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    [Fact]
    public async Task Postgres_upgrades_from_the_current_schema()
    {
        if (postgres is null)
        {
            return;
        }

        var schema = $"attachment_upgrade_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{schema}\"";
            await command.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            SearchPath = schema,
        }.ConnectionString;
        await using var database = new SegarisDesignTimeDbContextFactory().CreateDbContext(
        [
            "--provider",
            "Postgres",
            "--connection",
            connectionString,
        ]);
        var previousMigration = database.Database.GetMigrations()
            .Single(migration => migration.EndsWith("_ConfigurationFoundation", StringComparison.Ordinal));
        var migrator = database.GetService<IMigrator>();

        await migrator.MigrateAsync(previousMigration);
        await migrator.MigrateAsync();

        var applied = await database.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, migration => migration.EndsWith("_ConfigurationFoundation"));
        Assert.Contains(applied, migration => migration.EndsWith("_CapexDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_CatalogModelAndInitialization"));
        Assert.Contains(applied, migration => migration.EndsWith("_OpexDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_InventoryDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_TravelDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_MoodDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_ClothesDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_AssetsDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_MaintenanceDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_ProjectsDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_ProcessesDomainPersistence"));
        Assert.Contains(applied, migration => migration.EndsWith("_FirebirdDomainPersistence"));
        await database.Database.OpenConnectionAsync();
        await using var countCommand = database.Database.GetDbConnection().CreateCommand();
        // Three catalog tables plus the one-time initialization table.
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'configuration_%'";
        Assert.Equal(4L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'capex_%'";
        Assert.Equal(3L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'opex_%'";
        Assert.Equal(3L, (long)(await countCommand.ExecuteScalarAsync())!);
        // Items, item-suppliers, orders, order lines, and the category and location
        // catalogs.
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'inventory_%'";
        Assert.Equal(6L, (long)(await countCommand.ExecuteScalarAsync())!);
        // Trips, itinerary entries, expenses, and the trip-type and expense-category
        // catalogs.
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'travel_%'";
        Assert.Equal(5L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'mood_%'";
        Assert.Equal(1L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND (table_name LIKE 'clothes_%' OR table_name LIKE 'clothing_%')";
        Assert.Equal(4L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND (table_name = 'assets' OR table_name LIKE 'asset_%')";
        Assert.Equal(3L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'maintenance_%'";
        Assert.Equal(2L, (long)(await countCommand.ExecuteScalarAsync())!);
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'projects_%'";
        Assert.Equal(6L, (long)(await countCommand.ExecuteScalarAsync())!);
        // Processes plus the steps and category catalog tables.
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'processes_%'";
        Assert.Equal(3L, (long)(await countCommand.ExecuteScalarAsync())!);
        // People, usernames, interactions, and the category and platform catalogs.
        countCommand.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name LIKE 'firebird_%'";
        Assert.Equal(5L, (long)(await countCommand.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Postgres_backup_generates_a_valid_package()
    {
        if (postgres is null)
        {
            return;
        }

        if (!PgDumpAvailable() && !IsContinuousIntegration())
        {
            // The real dump path needs the PostgreSQL client tools. CI provides them; a
            // developer machine without them skips rather than fails.
            return;
        }

        const string adminUserName = "pg-backup-admin";
        const string adminPassword = "PgBackupPass123!";
        var attachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-pg-backup-att-{Guid.NewGuid():N}");
        var backupsPath = Path.Combine(Path.GetTempPath(), $"segaris-pg-backup-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(attachmentsPath);
        Directory.CreateDirectory(Path.Combine(attachmentsPath, "capex"));
        await File.WriteAllTextAsync(
            Path.Combine(attachmentsPath, "capex", "demo.txt"),
            "attachment payload");
        try
        {
            await using var factory = CreateFactory(
                new("Segaris:Storage:AttachmentsPath", attachmentsPath),
                new("Segaris:Storage:BackupsPath", backupsPath),
                new("Segaris:Identity:Bootstrap:UserName", adminUserName),
                new("Segaris:Identity:Bootstrap:Password", adminPassword));
            using var admin = factory.CreateClient();
            using var login = await admin.PostAsJsonAsync(
                "/api/session",
                new { userName = adminUserName, password = adminPassword },
                CancellationToken.None);
            login.EnsureSuccessStatusCode();

            using var start = await PostWithCsrfAsync(admin, "/api/backup-jobs", new { });
            Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
            var started = await start.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
            var id = started.GetProperty("id").GetInt32();

            var terminal = await PollUntilTerminalAsync(admin, id);
            Assert.Equal("Succeeded", terminal.GetProperty("state").GetString());
            Assert.Equal("segaris-backup.tar", terminal.GetProperty("resultReference").GetString());

            var packagePath = Path.Combine(backupsPath, "segaris-backup.tar");
            Assert.True(File.Exists(packagePath));

            var entries = new List<string>();
            await using (var stream = File.OpenRead(packagePath))
            using (var reader = new TarReader(stream))
            {
                while (reader.GetNextEntry() is { } entry)
                {
                    entries.Add(entry.Name);
                }
            }

            Assert.Contains("database.dump", entries);
            Assert.Contains("manifest.json", entries);
            Assert.Contains(entries, name => name.StartsWith("attachments/", StringComparison.Ordinal));
        }
        finally
        {
            TryDeleteDirectory(attachmentsPath);
            TryDeleteDirectory(backupsPath);
        }
    }

    private static async Task<JsonElement> PollUntilTerminalAsync(HttpClient client, int id)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            var status = await client.GetFromJsonAsync<JsonElement>(
                $"/api/backup-jobs/{id}",
                CancellationToken.None);
            var state = status.GetProperty("state").GetString();
            if (state is "Succeeded" or "Failed" or "Cancelled" or "Interrupted")
            {
                return status;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Backup job {id} did not reach a terminal state.");
    }

    private static bool PgDumpAvailable()
    {
        try
        {
            using var process = SystemProcess.Start(new ProcessStartInfo
            {
                FileName = "pg_dump",
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private WebApplicationFactory<Program> CreateFactory(
        params KeyValuePair<string, string?>[] additionalSettings)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    var settings = new List<KeyValuePair<string, string?>>
                    {
                        new("Segaris:Database:Provider", "Postgres"),
                        new("ConnectionStrings:Segaris", postgres!.GetConnectionString()),
                    };
                    settings.AddRange(additionalSettings);
                    configuration.AddInMemoryCollection(settings);
                });
            });
    }

    private static async Task<HttpResponseMessage> PostWithCsrfAsync(
        HttpClient client,
        string url,
        object body)
    {
        var token = await client.GetFromJsonAsync<JsonElement>(
            "/api/session/antiforgery",
            CancellationToken.None);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("csrfToken").GetString());
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static bool IsContinuousIntegration()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
