using System.Globalization;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Capex.Mutations;
using Segaris.Api.Modules.Capex.Queries;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Maps the Capex HTTP surface: the Wave 3 read endpoints (category catalog,
/// paginated Entries list, and entry detail) and the Wave 4 mutation, deletion,
/// and entry-scoped attachment routes frozen in <see cref="CapexApiRoutes"/>.
/// State-changing routes carry antiforgery protection and never expose EF Core
/// entities.
/// </summary>
internal static class CapexEndpoints
{
    public static void MapCapexEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("capex", CapexApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListCapexCategories")
            .WithSummary("Returns the Capex category catalog")
            .Produces<IReadOnlyList<CapexCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateCapexCategory").WithSummary("Creates a category at the end of the catalog").Produces<CapexCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(CapexApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateCapexCategory").WithSummary("Updates a Capex category").Produces<CapexCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(CapexApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveCapexCategory").WithSummary("Moves a Capex category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(CapexApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetCapexCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(CapexApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteCapexCategory").WithSummary("Deletes an unreferenced Capex category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/entries", ListEntriesAsync)
            .WithName("ListCapexEntries")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible entries")
            .Produces<PaginatedResponse<CapexEntrySummaryResponse>>();

        group.MapGet("/entries/{entryId:int}", GetEntryAsync)
            .WithName("GetCapexEntry")
            .WithSummary("Returns the detail of an accessible entry with its ordered items and attachments")
            .Produces<CapexEntryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/entries", CreateEntryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateCapexEntry")
            .WithSummary("Creates an entry, recalculating line and total amounts server-side")
            .Produces<CapexEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/entries/{entryId:int}", UpdateEntryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateCapexEntry")
            .WithSummary("Replaces an entry and its ordered items in one transaction")
            .Produces<CapexEntryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/entries/{entryId:int}", DeleteEntryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteCapexEntry")
            .WithSummary("Physically deletes an entry, its items, and its attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/entries/{entryId:int}/attachments", ListAttachmentsAsync)
            .WithName("ListCapexEntryAttachments")
            .WithSummary("Lists the attachments of an accessible entry")
            .Produces<IReadOnlyList<CapexAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/entries/{entryId:int}/attachments", UploadAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadCapexEntryAttachment")
            .WithSummary("Uploads one attachment for an accessible entry")
            .Produces<CapexAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/entries/{entryId:int}/attachments/{attachmentId:int}", DownloadAttachmentAsync)
            .WithName("DownloadCapexEntryAttachment")
            .WithSummary("Downloads one attachment of an accessible entry")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/entries/{entryId:int}/attachments/{attachmentId:int}", DeleteAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteCapexEntryAttachment")
            .WithSummary("Removes one attachment of an accessible entry")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListCategoriesAsync(
        CapexReadService read,
        CancellationToken cancellationToken)
    {
        var categories = await read.ListCategoriesAsync(cancellationToken);
        return TypedResults.Ok(categories);
    }

    private static UserId CategoryActor(ICurrentUser currentUser) => currentUser.UserId ?? throw CapexCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw CapexCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, CapexCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CategoryActor(user), token);
        return TypedResults.Created($"/api/capex/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, CapexCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CategoryActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, CapexCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, CapexCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, CapexCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListEntriesAsync(
        [AsParameters] CapexEntryListQuery query,
        CapexReadService read,
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

        var result = await read.ListEntriesAsync(filter, pagination, sort, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetEntryAsync(
        int entryId,
        CapexReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var entry = await read.GetEntryAsync(entryId, userId, cancellationToken);
        if (entry is null)
        {
            throw CapexProblem.EntryNotFound();
        }

        return TypedResults.Ok(entry);
    }

    private static async Task<IResult> CreateEntryAsync(
        CreateCapexEntryRequest request,
        CapexEntryWriteService write,
        CapexReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int entryId;
        try
        {
            entryId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (CapexValidationException exception)
        {
            throw CapexProblem.From(exception);
        }

        var created = await read.GetEntryAsync(entryId, userId, cancellationToken);
        return TypedResults.Created($"/api/capex/entries/{entryId}", created);
    }

    private static async Task<IResult> UpdateEntryAsync(
        int entryId,
        UpdateCapexEntryRequest request,
        CapexEntryWriteService write,
        CapexReadService read,
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
            updated = await write.UpdateAsync(entryId, request, userId, cancellationToken);
        }
        catch (CapexValidationException exception)
        {
            throw CapexProblem.From(exception);
        }

        if (!updated)
        {
            throw CapexProblem.EntryNotFound();
        }

        var entry = await read.GetEntryAsync(entryId, userId, cancellationToken);
        return TypedResults.Ok(entry);
    }

    private static async Task<IResult> DeleteEntryAsync(
        int entryId,
        CapexEntryWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(entryId, userId, cancellationToken);
        if (!deleted)
        {
            throw CapexProblem.EntryNotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListAttachmentsAsync(
        int entryId,
        CapexReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.EntryAccessibleAsync(entryId, userId, cancellationToken))
        {
            throw CapexProblem.EntryNotFound();
        }

        var descriptors = await attachments.ListByOwnerAsync(CapexAttachments.Owner(entryId), cancellationToken);
        return TypedResults.Ok(descriptors.Select(ToAttachment).ToArray());
    }

    private static async Task<IResult> UploadAttachmentAsync(
        int entryId,
        HttpRequest request,
        CapexReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.EntryAccessibleAsync(entryId, userId, cancellationToken))
        {
            throw CapexProblem.EntryNotFound();
        }

        if (!request.HasFormContentType)
        {
            throw CapexProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw CapexProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        await using var stream = file.OpenReadStream();
        AttachmentDescriptor created;
        try
        {
            created = await attachments.CreateAsync(
                new(CapexAttachments.Owner(entryId), file.FileName, file.ContentType, stream),
                userId,
                cancellationToken);
        }
        catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            // Surface platform file-validation failures under the Capex attachment
            // code while preserving the platform's field-level detail.
            throw CapexProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
        }

        return TypedResults.Created(
            $"/api/capex/entries/{entryId}/attachments/{created.Id.Value}",
            ToAttachment(created));
    }

    private static async Task<IResult> DownloadAttachmentAsync(
        int entryId,
        int attachmentId,
        CapexReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.EntryAccessibleAsync(entryId, userId, cancellationToken))
        {
            throw CapexProblem.EntryNotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            CapexAttachments.Owner(entryId),
            cancellationToken);
        if (download is null)
        {
            throw CapexProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteAttachmentAsync(
        int entryId,
        int attachmentId,
        CapexReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.EntryAccessibleAsync(entryId, userId, cancellationToken))
        {
            throw CapexProblem.EntryNotFound();
        }

        var removed = await attachments.DeleteAsync(
            new(attachmentId),
            CapexAttachments.Owner(entryId),
            cancellationToken);
        if (!removed)
        {
            throw CapexProblem.AttachmentNotFound();
        }

        return TypedResults.NoContent();
    }

    private static CapexAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt);
}
