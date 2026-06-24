using Segaris.Api.Composition;

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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCalendarEndpoints();
    }
}
