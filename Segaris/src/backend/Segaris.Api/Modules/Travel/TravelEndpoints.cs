namespace Segaris.Api.Modules.Travel;

internal static class TravelEndpoints
{
    public static IEndpointRouteBuilder MapTravelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGroup(TravelApiRoutes.Trips).WithTags(TravelApiRoutes.Tag);
        _ = endpoints.MapGroup(TravelApiRoutes.TripTypes).WithTags(TravelApiRoutes.Tag);
        _ = endpoints.MapGroup(TravelApiRoutes.ExpenseCategories).WithTags(TravelApiRoutes.Tag);

        return endpoints;
    }
}
