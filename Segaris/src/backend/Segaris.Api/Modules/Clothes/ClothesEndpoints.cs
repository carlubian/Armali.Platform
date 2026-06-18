using System.Globalization;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Api.Modules.Clothes.Mutations;
using Segaris.Api.Modules.Clothes.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Maps the Clothes HTTP surface. Wave 0 froze the routes as placeholders; Wave 1
/// exposes the module-owned category and colour catalog reads and the
/// administrator-only catalog management routes surfaced through Configuration. The
/// garment, attachment, and reference-migrating replace-and-delete routes remain
/// placeholders until the later waves that implement them. State-changing routes
/// carry antiforgery protection and never expose EF Core entities.
/// </summary>
internal static class ClothesEndpoints
{
    public static IEndpointRouteBuilder MapClothesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("clothes", ClothesApiRoutes.Tag)
            .RequireAuthorization();

        MapGarmentEndpoints(group);
        MapCategoryEndpoints(group);
        MapColorEndpoints(group);

        return endpoints;
    }

    private static void MapGarmentEndpoints(RouteGroupBuilder group)
    {
        var garments = group.MapGroup("/garments");
        garments.MapGet("", ListGarmentsAsync)
            .WithName("ListClothesGarments")
            .WithSummary("Returns a paginated, filtered, and sorted gallery of accessible Clothes garments")
            .Produces<PaginatedResponse<ClothesGarmentSummaryResponse>>();
        garments.MapPost("", CreateGarmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateClothesGarment")
            .WithSummary("Creates a Clothes garment with its colour set and care values")
            .Produces<ClothesGarmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        garments.MapGet(ClothesApiRoutes.GarmentById, GetGarmentAsync)
            .WithName("GetClothesGarment")
            .WithSummary("Returns the detail of an accessible Clothes garment")
            .Produces<ClothesGarmentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapPut(ClothesApiRoutes.GarmentById, UpdateGarmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateClothesGarment")
            .WithSummary("Replaces an accessible Clothes garment and its colour set")
            .Produces<ClothesGarmentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapDelete(ClothesApiRoutes.GarmentById, DeleteGarmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteClothesGarment")
            .WithSummary("Deletes an accessible Clothes garment and its owned attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapGet(ClothesApiRoutes.GarmentAttachments, ListGarmentAttachmentsAsync)
            .WithName("ListClothesGarmentAttachments")
            .WithSummary("Lists the attachments of an accessible Clothes garment")
            .Produces<IReadOnlyList<ClothesAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapPost(ClothesApiRoutes.GarmentAttachments, UploadGarmentAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadClothesGarmentAttachment")
            .WithSummary("Uploads one attachment for an accessible Clothes garment")
            .Produces<ClothesAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapGet(ClothesApiRoutes.GarmentAttachmentById, DownloadGarmentAttachmentAsync)
            .WithName("DownloadClothesGarmentAttachment")
            .WithSummary("Downloads one attachment of an accessible Clothes garment")
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapDelete(ClothesApiRoutes.GarmentAttachmentById, DeleteGarmentAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteClothesGarmentAttachment")
            .WithSummary("Removes one attachment of an accessible Clothes garment, clearing the primary image when needed")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        garments.MapPut(ClothesApiRoutes.GarmentPrimaryAttachment, SetGarmentPrimaryAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetClothesGarmentPrimaryAttachment")
            .WithSummary("Marks one image attachment as the garment's primary image")
            .Produces<ClothesAttachmentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListClothingCategories")
            .WithSummary("Returns the Clothes category catalog")
            .Produces<IReadOnlyList<ClothingCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateClothingCategory").WithSummary("Creates a category at the end of the catalog").Produces<ClothingCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(ClothesApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateClothingCategory").WithSummary("Updates a Clothes category").Produces<ClothingCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(ClothesApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveClothingCategory").WithSummary("Moves a Clothes category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(ClothesApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetClothingCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(ClothesApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteClothingCategory").WithSummary("Deletes an unreferenced Clothes category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        // Reference-migrating deletion is delivered with the Configuration reference handlers in a later wave.
        categories.MapPost(ClothesApiRoutes.CategoryReplaceAndDelete, NotImplemented).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteClothingCategory");
    }

    private static void MapColorEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/colors", ListColorsAsync)
            .WithName("ListClothingColors")
            .WithSummary("Returns the Clothes colour catalog with each colour value")
            .Produces<IReadOnlyList<ClothingColorResponse>>();

        var colors = group.MapGroup("/colors").RequireAuthorization(IdentityPolicies.Admin);
        colors.MapPost("", CreateColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateClothingColor").WithSummary("Creates a colour at the end of the catalog").Produces<ClothingColorResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        colors.MapPut(ClothesApiRoutes.ColorById, UpdateColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateClothingColor").WithSummary("Updates a Clothes colour and its colour value").Produces<ClothingColorResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        colors.MapPost(ClothesApiRoutes.ColorMove, MoveColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveClothingColor").WithSummary("Moves a Clothes colour one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        colors.MapGet(ClothesApiRoutes.ColorDeletionImpact, ColorImpactAsync).WithName("GetClothingColorDeletionImpact").WithSummary("Returns privacy-neutral colour deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        colors.MapDelete(ClothesApiRoutes.ColorById, DeleteColorAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteClothingColor").WithSummary("Deletes an unreferenced Clothes colour").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        // Reference-migrating deletion is delivered with the Configuration reference handlers in a later wave.
        colors.MapPost(ClothesApiRoutes.ColorReplaceAndDelete, NotImplemented).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteClothingColor");
    }

    private static async Task<IResult> ListCategoriesAsync(ClothesReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListColorsAsync(ClothesReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListColorsAsync(cancellationToken));

    private static async Task<IResult> ListGarmentsAsync(
        [AsParameters] ClothesGarmentListQuery query,
        ClothesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListGarmentsAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetGarmentAsync(
        int garmentId,
        ClothesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var garment = await read.GetGarmentAsync(garmentId, userId, cancellationToken);
        if (garment is null)
        {
            throw ClothesGarmentProblem.NotFound();
        }

        return TypedResults.Ok(garment);
    }

    private static async Task<IResult> CreateGarmentAsync(
        CreateClothesGarmentRequest request,
        ClothesGarmentWriteService write,
        ClothesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int garmentId;
        try
        {
            garmentId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (ClothesValidationException exception)
        {
            throw ClothesGarmentProblem.From(exception);
        }

        var created = await read.GetGarmentAsync(garmentId, userId, cancellationToken);
        return TypedResults.Created($"/api/clothes/garments/{garmentId}", created);
    }

    private static async Task<IResult> UpdateGarmentAsync(
        int garmentId,
        UpdateClothesGarmentRequest request,
        ClothesGarmentWriteService write,
        ClothesReadService read,
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
            updated = await write.UpdateAsync(garmentId, request, userId, cancellationToken);
        }
        catch (ClothesValidationException exception)
        {
            throw ClothesGarmentProblem.From(exception);
        }

        if (!updated)
        {
            throw ClothesGarmentProblem.NotFound();
        }

        var garment = await read.GetGarmentAsync(garmentId, userId, cancellationToken);
        return TypedResults.Ok(garment);
    }

    private static async Task<IResult> DeleteGarmentAsync(
        int garmentId,
        ClothesGarmentWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(garmentId, userId, cancellationToken);
        if (!deleted)
        {
            throw ClothesGarmentProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListGarmentAttachmentsAsync(
        int garmentId,
        ClothesReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var attachments = await read.ListGarmentAttachmentsAsync(garmentId, userId, cancellationToken);
        if (attachments is null)
        {
            throw ClothesGarmentProblem.NotFound();
        }

        return TypedResults.Ok(attachments);
    }

    private static async Task<IResult> UploadGarmentAttachmentAsync(
        int garmentId,
        HttpRequest request,
        ClothesReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.GarmentAccessibleAsync(garmentId, userId, cancellationToken))
        {
            throw ClothesGarmentProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw ClothesGarmentProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw ClothesGarmentProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        AttachmentDescriptor created;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                created = await attachments.CreateAsync(
                    new(ClothesAttachments.GarmentOwner(garmentId), file.FileName, file.ContentType, stream),
                    userId,
                    cancellationToken);
            }
            catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
            {
                throw ClothesGarmentProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
            }
        }

        return TypedResults.Created(
            $"/api/clothes/garments/{garmentId}/attachments/{created.Id.Value}",
            ToAttachment(created, isPrimary: false));
    }

    private static async Task<IResult> DownloadGarmentAttachmentAsync(
        int garmentId,
        int attachmentId,
        ClothesReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.GarmentAccessibleAsync(garmentId, userId, cancellationToken))
        {
            throw ClothesGarmentProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            ClothesAttachments.GarmentOwner(garmentId),
            cancellationToken);
        if (download is null)
        {
            throw ClothesGarmentProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteGarmentAttachmentAsync(
        int garmentId,
        int attachmentId,
        ClothesGarmentWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var outcome = await write.DeleteAttachmentAsync(garmentId, attachmentId, userId, cancellationToken);
        return outcome switch
        {
            ClothesDeleteAttachmentOutcome.GarmentNotFound => throw ClothesGarmentProblem.NotFound(),
            ClothesDeleteAttachmentOutcome.AttachmentNotFound => throw ClothesGarmentProblem.AttachmentNotFound(),
            _ => TypedResults.NoContent(),
        };
    }

    private static async Task<IResult> SetGarmentPrimaryAttachmentAsync(
        int garmentId,
        int attachmentId,
        ClothesGarmentWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await write.SetPrimaryAttachmentAsync(garmentId, attachmentId, userId, cancellationToken);
        return result.Outcome switch
        {
            ClothesSetPrimaryOutcome.GarmentNotFound => throw ClothesGarmentProblem.NotFound(),
            ClothesSetPrimaryOutcome.AttachmentNotFound => throw ClothesGarmentProblem.AttachmentNotFound(),
            ClothesSetPrimaryOutcome.NotImage => throw ClothesGarmentProblem.PrimaryNotImage(),
            _ => TypedResults.Ok(ToAttachment(result.Descriptor!, isPrimary: true)),
        };
    }

    private static ClothesAttachmentResponse ToAttachment(AttachmentDescriptor descriptor, bool isPrimary) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt,
        isPrimary);

    private static UserId CategoryActor(ICurrentUser currentUser) => currentUser.UserId ?? throw ClothesCategoryProblem.NotFound();

    private static UserId ColorActor(ICurrentUser currentUser) => currentUser.UserId ?? throw ClothesColorProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw ClothesCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection ColorDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw ClothesColorProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, ClothingCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CategoryActor(user), token);
        return TypedResults.Created($"/api/clothes/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, ClothingCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CategoryActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, ClothingCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, ClothingCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, ClothingCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateColorAsync(CatalogItemRequest request, ClothingColorManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, ColorActor(user), token);
        return TypedResults.Created($"/api/clothes/colors/{value.Id}", value);
    }

    private static async Task<IResult> UpdateColorAsync(int colorId, CatalogItemRequest request, ClothingColorManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(colorId, request, ColorActor(user), token));

    private static async Task<IResult> MoveColorAsync(int colorId, CatalogMoveRequest request, ClothingColorManagementService service, CancellationToken token)
    {
        await service.MoveAsync(colorId, ColorDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ColorImpactAsync(int colorId, ClothingColorManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(colorId, token));

    private static async Task<IResult> DeleteColorAsync(int colorId, ClothingColorManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(colorId, token);
        return TypedResults.NoContent();
    }

    private static IResult NotImplemented() =>
        Results.StatusCode(StatusCodes.Status501NotImplemented);
}
