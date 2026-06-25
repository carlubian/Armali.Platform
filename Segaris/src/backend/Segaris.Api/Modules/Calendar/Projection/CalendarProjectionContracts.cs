using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Travel.Contracts;

namespace Segaris.Api.Modules.Calendar.Projection;

internal static class CalendarProjectionContracts
{
    public static readonly IReadOnlyList<Type> InitialProviderContracts =
    [
        typeof(IFirebirdCalendarProjectionProvider),
        typeof(ITravelCalendarProjectionProvider),
        typeof(IInventoryCalendarProjectionProvider),
        typeof(IAssetsCalendarProjectionProvider),
        typeof(IMaintenanceCalendarProjectionProvider),
        typeof(IProcessesCalendarProjectionProvider),
    ];
}

internal sealed record CalendarProjectionProviderSet(
    IFirebirdCalendarProjectionProvider? Firebird,
    ITravelCalendarProjectionProvider? Travel,
    IInventoryCalendarProjectionProvider? Inventory,
    IAssetsCalendarProjectionProvider? Assets,
    IMaintenanceCalendarProjectionProvider? Maintenance,
    IProcessesCalendarProjectionProvider? Processes);
