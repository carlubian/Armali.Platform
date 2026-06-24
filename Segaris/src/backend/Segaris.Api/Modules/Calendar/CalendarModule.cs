using Segaris.Api.Composition;
using Segaris.Api.Modules.Calendar.Mutations;
using Segaris.Api.Modules.Calendar.Persistence;
using Segaris.Api.Modules.Calendar.Queries;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Calendar;

/// <summary>
/// Cross-domain read module for date-bound projections and Calendar-owned daily
/// notes. Wave 0 registers the shell and freezes the public contracts; later waves
/// add daily-note persistence, aggregation, source providers, and UI.
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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCalendarEndpoints();
    }
}
