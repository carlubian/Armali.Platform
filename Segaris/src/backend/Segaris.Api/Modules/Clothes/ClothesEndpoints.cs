using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Maps the Clothes HTTP surface frozen in Wave 0. Later waves replace these
/// placeholders with catalogue, garment, and attachment behaviour.
/// </summary>
internal static class ClothesEndpoints
{
    public static IEndpointRouteBuilder MapClothesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("clothes", ClothesApiRoutes.Tag)
            .RequireAuthorization();

        var garments = group.MapGroup("/garments");
        garments.MapGet("", NotImplemented).WithName("ListClothesGarments");
        garments.MapPost("", NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateClothesGarment");
        garments.MapGet(ClothesApiRoutes.GarmentById, NotImplemented).WithName("GetClothesGarment");
        garments.MapPut(ClothesApiRoutes.GarmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateClothesGarment");
        garments.MapDelete(ClothesApiRoutes.GarmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteClothesGarment");
        garments.MapGet(ClothesApiRoutes.GarmentAttachments, NotImplemented).WithName("ListClothesGarmentAttachments");
        garments.MapPost(ClothesApiRoutes.GarmentAttachments, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UploadClothesGarmentAttachment");
        garments.MapGet(ClothesApiRoutes.GarmentAttachmentById, NotImplemented).WithName("DownloadClothesGarmentAttachment");
        garments.MapDelete(ClothesApiRoutes.GarmentAttachmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteClothesGarmentAttachment");
        garments.MapPut(ClothesApiRoutes.GarmentPrimaryAttachment, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetClothesGarmentPrimaryAttachment");

        group.MapGet("/categories", NotImplemented).WithName("ListClothingCategories");
        group.MapGet("/colors", NotImplemented).WithName("ListClothingColors");

        return endpoints;
    }

    private static IResult NotImplemented() =>
        Results.StatusCode(StatusCodes.Status501NotImplemented);
}
