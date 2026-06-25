using Segaris.Api.Composition;
using Segaris.Api.Modules.Calendar.Mutations;
using Segaris.Api.Modules.Calendar.Persistence;
using Segaris.Api.Modules.Calendar.Projection;
using Segaris.Api.Modules.Calendar.Queries;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Calendar;

/// <summary>
/// Cross-domain read module for date-bound projections and Calendar-owned daily
/// notes. Wave 0 registered the shell and froze the public contracts; Wave 1 added
/// daily-note persistence and APIs; Wave 2 added the aggregation endpoint; Wave 3
/// wires the six source projection providers through their published contracts and
/// the Calendar adapters registered here. Later waves add the UI.
/// </summary>
internal sealed class CalendarModule : ISegarisModule
{
    public string Name => "Calendar";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, CalendarModelContributor>();
        services.AddScoped<CalendarEntriesReadService>();
        services.AddScoped<CalendarDailyNoteReadService>();
        services.AddScoped<CalendarDailyNoteWriteService>();

        // Each adapter wraps a source module's published projection contract and maps it
        // into the Calendar normalized projection. The source providers themselves are
        // registered by their owning modules.
        services.AddScoped<ICalendarProjectionProvider, FirebirdCalendarProjectionAdapter>();
        services.AddScoped<ICalendarProjectionProvider, TravelCalendarProjectionAdapter>();
        services.AddScoped<ICalendarProjectionProvider, InventoryCalendarProjectionAdapter>();
        services.AddScoped<ICalendarProjectionProvider, AssetsCalendarProjectionAdapter>();
        services.AddScoped<ICalendarProjectionProvider, MaintenanceCalendarProjectionAdapter>();
        services.AddScoped<ICalendarProjectionProvider, ProcessesCalendarProjectionAdapter>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCalendarEndpoints();
    }
}
