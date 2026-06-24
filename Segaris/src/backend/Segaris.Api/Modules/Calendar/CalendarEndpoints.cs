using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Calendar;

internal static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSegarisApiGroup(CalendarApiRoutes.Calendar, CalendarApiRoutes.Tag)
            .RequireAuthorization();

        return endpoints;
    }
}
