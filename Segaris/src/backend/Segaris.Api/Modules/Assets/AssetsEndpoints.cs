using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Maps the Assets HTTP surface frozen in Wave 0. Later waves replace these
/// placeholders with the required category and location catalogues, asset, and
/// attachment behaviour. State-changing routes carry antiforgery protection.
/// </summary>
internal static class AssetsEndpoints
{
    public static IEndpointRouteBuilder MapAssetsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("assets", AssetsApiRoutes.Tag)
            .RequireAuthorization();

        var items = group.MapGroup("/items");
        items.MapGet("", NotImplemented).WithName("ListAssets");
        items.MapPost("", NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateAsset");
        items.MapGet(AssetsApiRoutes.ItemById, NotImplemented).WithName("GetAsset");
        items.MapPut(AssetsApiRoutes.ItemById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateAsset");
        items.MapDelete(AssetsApiRoutes.ItemById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteAsset");
        items.MapGet(AssetsApiRoutes.ItemAttachments, NotImplemented).WithName("ListAssetAttachments");
        items.MapPost(AssetsApiRoutes.ItemAttachments, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UploadAssetAttachment");
        items.MapGet(AssetsApiRoutes.ItemAttachmentById, NotImplemented).WithName("DownloadAssetAttachment");
        items.MapDelete(AssetsApiRoutes.ItemAttachmentById, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteAssetAttachment");
        items.MapPut(AssetsApiRoutes.ItemPrimaryAttachment, NotImplemented)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetAssetPrimaryAttachment");

        group.MapGet("/categories", NotImplemented).WithName("ListAssetCategories");
        group.MapGet("/locations", NotImplemented).WithName("ListAssetLocations");

        return endpoints;
    }

    private static IResult NotImplemented() =>
        Results.StatusCode(StatusCodes.Status501NotImplemented);
}
