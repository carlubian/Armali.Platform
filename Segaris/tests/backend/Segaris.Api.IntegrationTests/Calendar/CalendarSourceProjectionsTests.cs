using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Calendar.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Firebird.Domain;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Calendar;

/// <summary>
/// Exercises the Wave 3 source projection providers end to end through the Calendar
/// aggregation endpoint, seeding source-module records directly so each projection rule
/// (status exclusions, date-range boundaries, target-route shape) and the cross-user
/// privacy boundary are verified against the real providers and adapters.
/// </summary>
public sealed class CalendarSourceProjectionsTests
{
    private const string EntriesPath = "/api/calendar/entries";
    private static readonly DateOnly RangeFrom = new(2026, 6, 1);
    private static readonly DateOnly RangeTo = new(2026, 6, 30);

    [Fact]
    public async Task Entries_project_each_source_with_correct_shape_and_exclusions()
    {
        using var server = new CapexTestServer();
        var ownerId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var seeded = await SeedSourceRecordsAsync(server, new UserId(ownerId), RecordVisibility.Public);

        using var client = await server.CreateAuthenticatedClientAsync();
        var entries = await GetEntriesAsync(client);
        var byId = entries.ToDictionary(entry => entry.Id);

        // Firebird birthday: family, occurrence date, and the people target route.
        var birthdayId = $"firebird:birthday:{seeded.BirthdayPersonId}:2026-06-24";
        Assert.True(byId.TryGetValue(birthdayId, out var birthday), $"Missing {birthdayId}");
        Assert.Equal("firebird", birthday!.SourceModule);
        Assert.Equal("Birthday", birthday.VisualFamily);
        Assert.Equal(new DateOnly(2026, 6, 24), birthday.StartDate);
        Assert.Equal($"/people?personId={seeded.BirthdayPersonId}", birthday.TargetRoute);

        // Travel trip: continuous all-day range, status, and trip target route.
        var tripId = $"travel:trip:{seeded.TripId}";
        Assert.True(byId.TryGetValue(tripId, out var trip), $"Missing {tripId}");
        Assert.Equal("Travel", trip!.VisualFamily);
        Assert.Equal(new DateOnly(2026, 6, 10), trip.StartDate);
        Assert.Equal(new DateOnly(2026, 6, 15), trip.EndDate);
        Assert.Equal("Planned", trip.Status);
        Assert.Equal($"/travel?tripId={seeded.TripId}", trip.TargetRoute);

        // Inventory expected receipt.
        var orderId = $"inventory:order:{seeded.OrderId}";
        Assert.True(byId.TryGetValue(orderId, out var order), $"Missing {orderId}");
        Assert.Equal("Other", order!.VisualFamily);
        Assert.Equal("Planning", order.Status);
        Assert.Equal(new DateOnly(2026, 6, 20), order.StartDate);
        Assert.Equal($"/inventory?orderId={seeded.OrderId}", order.TargetRoute);

        // Assets expected end of life, including the inclusive lower boundary.
        Assert.Contains($"assets:asset:{seeded.AssetId}", byId.Keys);
        Assert.Contains($"assets:asset:{seeded.AssetBoundaryId}", byId.Keys);
        var asset = byId[$"assets:asset:{seeded.AssetId}"];
        Assert.Equal("Other", asset.VisualFamily);
        Assert.Equal($"/assets?assetId={seeded.AssetId}", asset.TargetRoute);

        // Maintenance due date.
        var taskId = $"maintenance:task:{seeded.TaskId}";
        Assert.True(byId.TryGetValue(taskId, out var task), $"Missing {taskId}");
        Assert.Equal("Pending", task!.Status);
        Assert.Equal($"/maintenance?taskId={seeded.TaskId}", task.TargetRoute);

        // Processes pending step due date with process context as the subtitle.
        var stepId = $"processes:step:{seeded.StepId}";
        Assert.True(byId.TryGetValue(stepId, out var step), $"Missing {stepId}");
        Assert.Equal("Other", step!.VisualFamily);
        Assert.Equal("Sign form", step.Title);
        Assert.Equal("Onboard", step.Subtitle);
        Assert.Equal(new DateOnly(2026, 6, 22), step.StartDate);
        Assert.Equal($"/processes?processId={seeded.ProcessId}&steps=true", step.TargetRoute);

        // Status, date-range, and rule exclusions never reach Calendar.
        Assert.DoesNotContain($"firebird:birthday:{seeded.NoBirthdayPersonId}:2026-06-24", byId.Keys);
        Assert.DoesNotContain($"travel:trip:{seeded.CancelledTripId}", byId.Keys);
        Assert.DoesNotContain($"inventory:order:{seeded.ReceivedOrderId}", byId.Keys);
        Assert.DoesNotContain($"assets:asset:{seeded.RetiredAssetId}", byId.Keys);
        Assert.DoesNotContain($"assets:asset:{seeded.OutOfRangeAssetId}", byId.Keys);
        Assert.DoesNotContain($"maintenance:task:{seeded.CompletedTaskId}", byId.Keys);
        Assert.DoesNotContain($"processes:step:{seeded.CancelledProcessStepId}", byId.Keys);
        Assert.DoesNotContain($"processes:step:{seeded.ResolvedStepId}", byId.Keys);
    }

    [Fact]
    public async Task Entries_exclude_another_users_private_source_records()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("calendar-source-other", "CalendarOther123!");
        var otherId = await server.GetUserIdAsync("calendar-source-other");
        var seeded = await SeedSourceRecordsAsync(server, new UserId(otherId), RecordVisibility.Private);

        using var client = await server.CreateAuthenticatedClientAsync();
        var entries = await GetEntriesAsync(client);
        var ids = entries.Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain($"firebird:birthday:{seeded.BirthdayPersonId}:2026-06-24", ids);
        Assert.DoesNotContain($"travel:trip:{seeded.TripId}", ids);
        Assert.DoesNotContain($"inventory:order:{seeded.OrderId}", ids);
        Assert.DoesNotContain($"assets:asset:{seeded.AssetId}", ids);
        Assert.DoesNotContain($"maintenance:task:{seeded.TaskId}", ids);
        Assert.DoesNotContain($"processes:step:{seeded.StepId}", ids);
    }

    private static async Task<IReadOnlyList<CalendarEntryResponse>> GetEntriesAsync(HttpClient client)
    {
        var entries = await client.GetFromJsonAsync<IReadOnlyList<CalendarEntryResponse>>(
            $"{EntriesPath}?from={RangeFrom:yyyy-MM-dd}&to={RangeTo:yyyy-MM-dd}",
            CancellationToken.None);
        Assert.NotNull(entries);
        return entries!;
    }

    private static async Task<SeededIds> SeedSourceRecordsAsync(
        CapexTestServer server,
        UserId owner,
        RecordVisibility visibility)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var personCategoryId = await db.Set<PersonCategory>().Select(row => row.Id).FirstAsync();
        var tripTypeId = await db.Set<TravelTripType>().Select(row => row.Id).FirstAsync();
        var inventoryCategoryId = await db.Set<InventoryCategory>().Select(row => row.Id).FirstAsync();
        var inventoryLocationId = await db.Set<InventoryLocation>().Select(row => row.Id).FirstAsync();
        var assetCategoryId = await db.Set<AssetCategory>().Select(row => row.Id).FirstAsync();
        var assetLocationId = await db.Set<AssetLocation>().Select(row => row.Id).FirstAsync();
        var maintenanceTypeId = await db.Set<MaintenanceType>().Select(row => row.Id).FirstAsync();
        var processCategoryId = await db.Set<ProcessCategory>().Select(row => row.Id).FirstAsync();
        var supplierId = await db.Set<SegarisSupplier>().Select(row => row.Id).FirstAsync();
        var currencyId = await db.Set<SegarisCurrency>().Select(row => row.Id).FirstAsync();

        // Firebird: an included June birthday and a person without a birthday.
        var birthdayPerson = Person.Create(
            new PersonValues("Birthday Person", personCategoryId, PersonStatus.Active, 6, 24, null, visibility),
            owner,
            now);
        var noBirthdayPerson = Person.Create(
            new PersonValues("No Birthday", personCategoryId, PersonStatus.Active, null, null, null, visibility),
            owner,
            now);
        db.Add(birthdayPerson);
        db.Add(noBirthdayPerson);

        // Travel: an included planned trip and an excluded cancelled trip.
        var trip = TravelTrip.Create(
            new TravelTripValues("Barcelona", tripTypeId, null, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15), TravelTripStatus.Planned, null, visibility, []),
            owner,
            now);
        var cancelledTrip = TravelTrip.Create(
            new TravelTripValues("Cancelled", tripTypeId, null, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 14), TravelTripStatus.Cancelled, null, visibility, []),
            owner,
            now);
        db.Add(trip);
        db.Add(cancelledTrip);

        // Assets: included, inclusive lower boundary, excluded retired, excluded out of range.
        var asset = Asset.Create(
            new AssetValues("Boiler", assetCategoryId, assetLocationId, AssetStatus.Active, null, null, null, null, new DateOnly(2026, 6, 5), null, visibility),
            owner,
            now);
        var boundaryAsset = Asset.Create(
            new AssetValues("Boundary", assetCategoryId, assetLocationId, AssetStatus.Stored, null, null, null, null, RangeFrom, null, visibility),
            owner,
            now);
        var retiredAsset = Asset.Create(
            new AssetValues("Retired", assetCategoryId, assetLocationId, AssetStatus.Retired, null, null, null, null, new DateOnly(2026, 6, 7), null, visibility),
            owner,
            now);
        var outOfRangeAsset = Asset.Create(
            new AssetValues("Future", assetCategoryId, assetLocationId, AssetStatus.Active, null, null, null, null, new DateOnly(2026, 7, 1), null, visibility),
            owner,
            now);
        db.Add(asset);
        db.Add(boundaryAsset);
        db.Add(retiredAsset);
        db.Add(outOfRangeAsset);

        // Maintenance: an included pending task and an excluded completed task.
        var task = MaintenanceTask.Create(
            new MaintenanceTaskValues("Fix tap", maintenanceTypeId, MaintenanceStatus.Pending, MaintenancePriority.Medium, new DateOnly(2026, 6, 18), null, null, visibility),
            owner,
            now,
            today);
        var completedTask = MaintenanceTask.Create(
            new MaintenanceTaskValues("Done", maintenanceTypeId, MaintenanceStatus.Completed, MaintenancePriority.Medium, new DateOnly(2026, 6, 19), null, null, visibility),
            owner,
            now,
            today);
        db.Add(task);
        db.Add(completedTask);

        // Inventory needs a saved item before an order line can reference it.
        var item = InventoryItem.Create(
            new InventoryItemValues("Widget", InventoryItemStatus.Active, null, inventoryCategoryId, inventoryLocationId, 0m, 0m, [supplierId], visibility),
            owner,
            now);
        db.Add(item);

        // Processes: included pending step, plus excluded cancelled-process and resolved steps.
        var process = Process.Create(new ProcessValues("Onboard", processCategoryId, null, null, visibility), owner, now);
        var cancelledProcess = Process.Create(new ProcessValues("Cancelled", processCategoryId, null, null, visibility), owner, now);
        cancelledProcess.Cancel(owner, now);
        var resolvedProcess = Process.Create(new ProcessValues("Resolved", processCategoryId, null, null, visibility), owner, now);
        db.Add(process);
        db.Add(cancelledProcess);
        db.Add(resolvedProcess);

        await db.SaveChangesAsync();

        // Orders and steps reference the now-persisted item and processes.
        var order = InventoryOrder.Create(
            new InventoryOrderValues(supplierId, InventoryOrderStatus.Planning, currencyId, null, new DateOnly(2026, 6, 20), null, visibility, [new InventoryOrderLineValues(item.Id, 1m, 10m)]),
            owner,
            now);
        var receivedOrder = InventoryOrder.Create(
            new InventoryOrderValues(supplierId, InventoryOrderStatus.Received, currencyId, null, new DateOnly(2026, 6, 21), null, visibility, [new InventoryOrderLineValues(item.Id, 1m, 10m)]),
            owner,
            now);
        db.Add(order);
        db.Add(receivedOrder);

        var step = Step.Create(process.Id, new StepValues("Sign form", new DateOnly(2026, 6, 22), null, false), 0, owner, now);
        var cancelledProcessStep = Step.Create(cancelledProcess.Id, new StepValues("Hidden", new DateOnly(2026, 6, 23), null, false), 0, owner, now);
        var resolvedStep = Step.Create(resolvedProcess.Id, new StepValues("Resolved", new DateOnly(2026, 6, 24), null, false), 0, owner, now);
        resolvedStep.Complete(owner, now);
        db.Add(step);
        db.Add(cancelledProcessStep);
        db.Add(resolvedStep);

        await db.SaveChangesAsync();

        return new SeededIds(
            birthdayPerson.Id,
            noBirthdayPerson.Id,
            trip.Id,
            cancelledTrip.Id,
            order.Id,
            receivedOrder.Id,
            asset.Id,
            boundaryAsset.Id,
            retiredAsset.Id,
            outOfRangeAsset.Id,
            task.Id,
            completedTask.Id,
            process.Id,
            step.Id,
            cancelledProcessStep.Id,
            resolvedStep.Id);
    }

    private sealed record SeededIds(
        int BirthdayPersonId,
        int NoBirthdayPersonId,
        int TripId,
        int CancelledTripId,
        int OrderId,
        int ReceivedOrderId,
        int AssetId,
        int AssetBoundaryId,
        int RetiredAssetId,
        int OutOfRangeAssetId,
        int TaskId,
        int CompletedTaskId,
        int ProcessId,
        int StepId,
        int CancelledProcessStepId,
        int ResolvedStepId);
}
