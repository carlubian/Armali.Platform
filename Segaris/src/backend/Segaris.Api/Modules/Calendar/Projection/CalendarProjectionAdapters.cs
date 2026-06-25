using System.Globalization;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Calendar.Projection;

/// <summary>
/// Adapters that map each source-owned projection contract into the Calendar normalized
/// projection model. Calendar owns the stable response identifier, source-module and
/// source-type codes, visual family, and the title/subtitle/status presentation; the
/// source module owns the underlying records and the safe target route. Keeping the
/// mapping here is what lets Calendar depend on each source module only through its
/// published <c>*.Contracts</c> namespace.
/// </summary>
internal static class CalendarProjectionId
{
    public static string Birthday(int personId, DateOnly occurrence) =>
        $"firebird:birthday:{personId.ToString(CultureInfo.InvariantCulture)}:{occurrence:yyyy-MM-dd}";

    public static string Trip(int tripId) =>
        $"travel:trip:{tripId.ToString(CultureInfo.InvariantCulture)}";

    public static string InventoryOrder(int orderId) =>
        $"inventory:order:{orderId.ToString(CultureInfo.InvariantCulture)}";

    public static string Asset(int assetId) =>
        $"assets:asset:{assetId.ToString(CultureInfo.InvariantCulture)}";

    public static string MaintenanceTask(int taskId) =>
        $"maintenance:task:{taskId.ToString(CultureInfo.InvariantCulture)}";

    public static string ProcessStep(int stepId) =>
        $"processes:step:{stepId.ToString(CultureInfo.InvariantCulture)}";
}

internal sealed class FirebirdCalendarProjectionAdapter(IFirebirdCalendarProjectionProvider provider)
    : ICalendarProjectionProvider
{
    public string SourceModule => CalendarSourceModules.Firebird;

    public async Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var birthdays = await provider.ListCalendarBirthdaysAsync(from, to, viewer, cancellationToken);
        return birthdays
            .Select(birthday => new NormalizedCalendarProjection(
                CalendarProjectionId.Birthday(birthday.PersonId, birthday.OccurrenceDate),
                CalendarSourceModules.Firebird,
                CalendarSourceTypes.Birthday,
                CalendarVisualFamilies.Birthday,
                birthday.PersonName,
                null,
                birthday.OccurrenceDate,
                null,
                true,
                null,
                birthday.TargetRoute))
            .ToArray();
    }
}

internal sealed class TravelCalendarProjectionAdapter(ITravelCalendarProjectionProvider provider)
    : ICalendarProjectionProvider
{
    public string SourceModule => CalendarSourceModules.Travel;

    public async Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var trips = await provider.ListCalendarTripsAsync(from, to, viewer, cancellationToken);
        return trips
            .Select(trip => new NormalizedCalendarProjection(
                CalendarProjectionId.Trip(trip.TripId),
                CalendarSourceModules.Travel,
                CalendarSourceTypes.Trip,
                CalendarVisualFamilies.Travel,
                trip.Name,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                true,
                trip.Status,
                trip.TargetRoute))
            .ToArray();
    }
}

internal sealed class InventoryCalendarProjectionAdapter(IInventoryCalendarProjectionProvider provider)
    : ICalendarProjectionProvider
{
    public string SourceModule => CalendarSourceModules.Inventory;

    public async Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var orders = await provider.ListCalendarExpectedReceiptsAsync(from, to, viewer, cancellationToken);
        return orders
            .Select(order => new NormalizedCalendarProjection(
                CalendarProjectionId.InventoryOrder(order.OrderId),
                CalendarSourceModules.Inventory,
                CalendarSourceTypes.InventoryOrderExpectedReceipt,
                CalendarVisualFamilies.Other,
                order.Title,
                null,
                order.ExpectedReceiptDate,
                null,
                true,
                order.Status,
                order.TargetRoute))
            .ToArray();
    }
}

internal sealed class AssetsCalendarProjectionAdapter(IAssetsCalendarProjectionProvider provider)
    : ICalendarProjectionProvider
{
    public string SourceModule => CalendarSourceModules.Assets;

    public async Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var assets = await provider.ListCalendarExpectedEndOfLifeAsync(from, to, viewer, cancellationToken);
        return assets
            .Select(asset => new NormalizedCalendarProjection(
                CalendarProjectionId.Asset(asset.AssetId),
                CalendarSourceModules.Assets,
                CalendarSourceTypes.AssetExpectedEndOfLife,
                CalendarVisualFamilies.Other,
                asset.Name,
                null,
                asset.ExpectedEndOfLifeDate,
                null,
                true,
                asset.Status,
                asset.TargetRoute))
            .ToArray();
    }
}

internal sealed class MaintenanceCalendarProjectionAdapter(IMaintenanceCalendarProjectionProvider provider)
    : ICalendarProjectionProvider
{
    public string SourceModule => CalendarSourceModules.Maintenance;

    public async Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var tasks = await provider.ListCalendarDueTasksAsync(from, to, viewer, cancellationToken);
        return tasks
            .Select(task => new NormalizedCalendarProjection(
                CalendarProjectionId.MaintenanceTask(task.TaskId),
                CalendarSourceModules.Maintenance,
                CalendarSourceTypes.MaintenanceTaskDue,
                CalendarVisualFamilies.Other,
                task.Title,
                null,
                task.DueDate,
                null,
                true,
                task.Status,
                task.TargetRoute))
            .ToArray();
    }
}

internal sealed class ProcessesCalendarProjectionAdapter(IProcessesCalendarProjectionProvider provider)
    : ICalendarProjectionProvider
{
    public string SourceModule => CalendarSourceModules.Processes;

    public async Task<IReadOnlyList<NormalizedCalendarProjection>> ListAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var steps = await provider.ListCalendarPendingStepDueDatesAsync(from, to, viewer, cancellationToken);
        return steps
            .Select(step => new NormalizedCalendarProjection(
                CalendarProjectionId.ProcessStep(step.StepId),
                CalendarSourceModules.Processes,
                CalendarSourceTypes.ProcessStepDue,
                CalendarVisualFamilies.Other,
                step.StepTitle,
                step.ProcessTitle,
                step.DueDate,
                null,
                true,
                null,
                step.TargetRoute))
            .ToArray();
    }
}
