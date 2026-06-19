using System.Globalization;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Assets.Mutations;
using Segaris.Api.Modules.Assets.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Maps the Assets HTTP surface. Wave 1 exposes the module-owned category and
/// location catalog reads and the administrator-only catalog management routes
/// surfaced through Configuration; later waves replace the asset and attachment
/// placeholders. State-changing routes carry antiforgery protection and never
/// expose EF Core entities.
/// </summary>
internal static class AssetsEndpoints
{
    public static IEndpointRouteBuilder MapAssetsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("assets", AssetsApiRoutes.Tag)
            .RequireAuthorization();

        MapItemEndpoints(group);
        MapCategoryEndpoints(group);
        MapLocationEndpoints(group);

        return endpoints;
    }

    private static void MapItemEndpoints(RouteGroupBuilder group)
    {
        var items = group.MapGroup("/items");
        items.MapGet("", ListAssetsAsync)
            .WithName("ListAssets")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible assets")
            .Produces<PaginatedResponse<AssetSummaryResponse>>();
        items.MapPost("", CreateAssetAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateAsset")
            .WithSummary("Creates an asset with catalog validation and a unique code")
            .Produces<AssetResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        items.MapGet(AssetsApiRoutes.ItemById, GetAssetAsync)
            .WithName("GetAsset")
            .WithSummary("Returns the detail of an accessible asset")
            .Produces<AssetResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        items.MapGet(AssetsApiRoutes.ItemDeletionImpact, GetAssetDeletionImpactAsync)
            .WithName("GetAssetDeletionImpact")
            .WithSummary("Returns privacy-neutral cross-module deletion impact for an accessible asset")
            .Produces<AssetDeletionImpactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        items.MapPut(AssetsApiRoutes.ItemById, UpdateAssetAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateAsset")
            .WithSummary("Replaces an accessible asset in one transaction")
            .Produces<AssetResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        items.MapDelete(AssetsApiRoutes.ItemById, DeleteAssetAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteAsset")
            .WithSummary("Physically deletes an unreferenced asset and its attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        items.MapPost(AssetsApiRoutes.ItemReassignAndDelete, ReassignAndDeleteAssetAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReassignAndDeleteAsset")
            .WithSummary("Atomically reassigns cross-module references and deletes an asset")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        items.MapGet(AssetsApiRoutes.ItemAttachments, ListAssetAttachmentsAsync)
            .WithName("ListAssetAttachments")
            .WithSummary("Lists the attachments of an accessible asset")
            .Produces<IReadOnlyList<AssetAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        items.MapPost(AssetsApiRoutes.ItemAttachments, UploadAssetAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadAssetAttachment")
            .WithSummary("Uploads one attachment for an accessible asset")
            .Produces<AssetAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        items.MapGet(AssetsApiRoutes.ItemAttachmentById, DownloadAssetAttachmentAsync)
            .WithName("DownloadAssetAttachment")
            .WithSummary("Downloads one attachment of an accessible asset")
            .ProducesProblem(StatusCodes.Status404NotFound);
        items.MapDelete(AssetsApiRoutes.ItemAttachmentById, DeleteAssetAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteAssetAttachment")
            .WithSummary("Removes one attachment of an accessible asset, clearing primary when needed")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        items.MapPut(AssetsApiRoutes.ItemPrimaryAttachment, SetAssetPrimaryAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetAssetPrimaryAttachment")
            .WithSummary("Marks one image attachment as the asset's primary image")
            .Produces<AssetAttachmentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListAssetCategories")
            .WithSummary("Returns the Assets category catalog")
            .Produces<IReadOnlyList<AssetCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateAssetCategory").WithSummary("Creates a category at the end of the catalog").Produces<AssetCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(AssetsApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateAssetCategory").WithSummary("Updates an Assets category").Produces<AssetCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(AssetsApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveAssetCategory").WithSummary("Moves an Assets category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(AssetsApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetAssetCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(AssetsApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteAssetCategory").WithSummary("Deletes an unreferenced Assets category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(AssetsApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteAssetCategory").WithSummary("Migrates references and deletes an Assets category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapLocationEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/locations", ListLocationsAsync)
            .WithName("ListAssetLocations")
            .WithSummary("Returns the Assets location catalog")
            .Produces<IReadOnlyList<AssetLocationResponse>>();

        var locations = group.MapGroup("/locations").RequireAuthorization(IdentityPolicies.Admin);
        locations.MapPost("", CreateLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateAssetLocation").WithSummary("Creates a location at the end of the catalog").Produces<AssetLocationResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPut(AssetsApiRoutes.LocationById, UpdateLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateAssetLocation").WithSummary("Updates an Assets location").Produces<AssetLocationResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPost(AssetsApiRoutes.LocationMove, MoveLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveAssetLocation").WithSummary("Moves an Assets location one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        locations.MapGet(AssetsApiRoutes.LocationDeletionImpact, LocationImpactAsync).WithName("GetAssetLocationDeletionImpact").WithSummary("Returns privacy-neutral location deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        locations.MapDelete(AssetsApiRoutes.LocationById, DeleteLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteAssetLocation").WithSummary("Deletes an unreferenced Assets location").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPost(AssetsApiRoutes.LocationReplaceAndDelete, ReplaceAndDeleteLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteAssetLocation").WithSummary("Migrates references and deletes an Assets location atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListAssetsAsync(
        [AsParameters] AssetListQuery query,
        AssetReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var pagination = query.ToPagination();
        var sort = query.ToSort();
        var filter = query.ToFilter();

        var result = await read.ListAssetsAsync(filter, pagination, sort, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetAssetAsync(
        int assetId,
        AssetReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var asset = await read.GetAssetAsync(assetId, userId, cancellationToken);
        if (asset is null)
        {
            throw AssetProblem.NotFound();
        }

        return TypedResults.Ok(asset);
    }

    private static async Task<IResult> CreateAssetAsync(
        CreateAssetRequest request,
        AssetWriteService write,
        AssetReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int assetId;
        try
        {
            assetId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (AssetValidationException exception)
        {
            throw AssetProblem.From(exception);
        }

        var created = await read.GetAssetAsync(assetId, userId, cancellationToken);
        return TypedResults.Created($"/api/assets/items/{assetId}", created);
    }

    private static async Task<IResult> UpdateAssetAsync(
        int assetId,
        UpdateAssetRequest request,
        AssetWriteService write,
        AssetReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool updated;
        try
        {
            updated = await write.UpdateAsync(assetId, request, userId, cancellationToken);
        }
        catch (AssetValidationException exception)
        {
            throw AssetProblem.From(exception);
        }

        if (!updated)
        {
            throw AssetProblem.NotFound();
        }

        var asset = await read.GetAssetAsync(assetId, userId, cancellationToken);
        return TypedResults.Ok(asset);
    }

    private static async Task<IResult> GetAssetDeletionImpactAsync(
        int assetId,
        AssetWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var impact = await write.GetDeletionImpactAsync(assetId, userId, cancellationToken);
        if (impact is null)
        {
            throw AssetProblem.NotFound();
        }

        return TypedResults.Ok(impact);
    }

    private static async Task<IResult> DeleteAssetAsync(
        int assetId,
        AssetWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool deleted;
        try
        {
            deleted = await write.DeleteAsync(assetId, userId, cancellationToken);
        }
        catch (AssetReassignmentBlockedException exception)
        {
            throw AssetProblem.ReassignmentBlocked(exception);
        }
        if (!deleted)
        {
            throw AssetProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReassignAndDeleteAssetAsync(
        int assetId,
        AssetReassignmentDeletionRequest request,
        AssetWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        AssetDeletionOutcome outcome;
        try
        {
            outcome = await write.ReassignAndDeleteAsync(assetId, request, userId, cancellationToken);
        }
        catch (AssetReassignmentBlockedException exception)
        {
            throw AssetProblem.ReassignmentBlocked(exception);
        }

        return outcome switch
        {
            AssetDeletionOutcome.Deleted => TypedResults.NoContent(),
            AssetDeletionOutcome.AssetNotFound => throw AssetProblem.NotFound(),
            AssetDeletionOutcome.InvalidReassignment => throw AssetProblem.InvalidReassignment(
                "Choose a different accessible target asset."),
            _ => throw AssetProblem.InvalidReassignment("The requested reassignment cannot be applied."),
        };
    }

    private static async Task<IResult> ListAssetAttachmentsAsync(
        int assetId,
        AssetReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var attachments = await read.ListAssetAttachmentsAsync(assetId, userId, cancellationToken);
        if (attachments is null)
        {
            throw AssetProblem.NotFound();
        }

        return TypedResults.Ok(attachments);
    }

    private static async Task<IResult> UploadAssetAttachmentAsync(
        int assetId,
        HttpRequest request,
        AssetReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.AssetAccessibleAsync(assetId, userId, cancellationToken))
        {
            throw AssetProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw AssetProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw AssetProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        AttachmentDescriptor created;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                created = await attachments.CreateAsync(
                    new(AssetsAttachments.AssetOwner(assetId), file.FileName, file.ContentType, stream),
                    userId,
                    cancellationToken);
            }
            catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
            {
                throw AssetProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
            }
        }

        return TypedResults.Created(
            $"/api/assets/items/{assetId}/attachments/{created.Id.Value}",
            ToAttachment(created, isPrimary: false));
    }

    private static async Task<IResult> DownloadAssetAttachmentAsync(
        int assetId,
        int attachmentId,
        AssetReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.AssetAccessibleAsync(assetId, userId, cancellationToken))
        {
            throw AssetProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            AssetsAttachments.AssetOwner(assetId),
            cancellationToken);
        if (download is null)
        {
            throw AssetProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteAssetAttachmentAsync(
        int assetId,
        int attachmentId,
        AssetWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var outcome = await write.DeleteAttachmentAsync(assetId, attachmentId, userId, cancellationToken);
        return outcome switch
        {
            AssetDeleteAttachmentOutcome.AssetNotFound => throw AssetProblem.NotFound(),
            AssetDeleteAttachmentOutcome.AttachmentNotFound => throw AssetProblem.AttachmentNotFound(),
            _ => TypedResults.NoContent(),
        };
    }

    private static async Task<IResult> SetAssetPrimaryAttachmentAsync(
        int assetId,
        int attachmentId,
        AssetWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await write.SetPrimaryAttachmentAsync(assetId, attachmentId, userId, cancellationToken);
        return result.Outcome switch
        {
            AssetSetPrimaryOutcome.AssetNotFound => throw AssetProblem.NotFound(),
            AssetSetPrimaryOutcome.AttachmentNotFound => throw AssetProblem.AttachmentNotFound(),
            AssetSetPrimaryOutcome.NotImage => throw AssetProblem.PrimaryNotImage(),
            _ => TypedResults.Ok(ToAttachment(result.Descriptor!, isPrimary: true)),
        };
    }

    private static AssetAttachmentResponse ToAttachment(AttachmentDescriptor descriptor, bool isPrimary) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt,
        isPrimary);

    private static async Task<IResult> ListCategoriesAsync(AssetReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListLocationsAsync(AssetReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListLocationsAsync(cancellationToken));

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw AssetCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw AssetCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection LocationDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw AssetLocationProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, AssetCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/assets/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, AssetCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, AssetCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, AssetCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, AssetCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(int categoryId, CatalogReplacementRequest request, AssetCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateLocationAsync(CatalogItemRequest request, AssetLocationManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/assets/locations/{value.Id}", value);
    }

    private static async Task<IResult> UpdateLocationAsync(int locationId, CatalogItemRequest request, AssetLocationManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(locationId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveLocationAsync(int locationId, CatalogMoveRequest request, AssetLocationManagementService service, CancellationToken token)
    {
        await service.MoveAsync(locationId, LocationDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> LocationImpactAsync(int locationId, AssetLocationManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(locationId, token));

    private static async Task<IResult> DeleteLocationAsync(int locationId, AssetLocationManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(locationId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteLocationAsync(int locationId, CatalogReplacementRequest request, AssetLocationManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(locationId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

}
