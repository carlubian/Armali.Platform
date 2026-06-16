using System.Globalization;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Api.Modules.Inventory.Mutations;
using Segaris.Api.Modules.Inventory.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory;

/// <summary>
/// Maps the Inventory HTTP surface. Wave 1 exposes the module-owned category and
/// location catalog reads and the administrator-only catalog management routes
/// surfaced through Configuration; Wave 2 adds the paginated item list, item detail,
/// and quick stock-adjustment routes; Wave 3 adds item mutation and attachment
/// routes; Wave 4 adds order read, mutation, and attachment routes. Wave 5 adds the
/// remaining receive route frozen in <see cref="InventoryApiRoutes"/>.
/// State-changing routes carry antiforgery protection and never expose EF Core
/// entities.
/// </summary>
internal static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("inventory", InventoryApiRoutes.Tag)
            .RequireAuthorization();

        MapItemEndpoints(group);
        MapOrderEndpoints(group);
        MapCategoryEndpoints(group);
        MapLocationEndpoints(group);
    }

    private static void MapItemEndpoints(RouteGroupBuilder group)
    {
        var items = group.MapGroup("/items");

        items.MapGet("", ListItemsAsync)
            .WithName("ListInventoryItems")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible Inventory items")
            .Produces<PaginatedResponse<InventoryItemSummaryResponse>>();

        items.MapGet(InventoryApiRoutes.ItemById, GetItemAsync)
            .WithName("GetInventoryItem")
            .WithSummary("Returns the detail of an accessible Inventory item with its suppliers and attachments")
            .Produces<InventoryItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapPost("", CreateItemAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateInventoryItem")
            .WithSummary("Creates an Inventory item with its allowed supplier set")
            .Produces<InventoryItemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        items.MapPut(InventoryApiRoutes.ItemById, UpdateItemAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateInventoryItem")
            .WithSummary("Replaces an accessible Inventory item and its allowed supplier set")
            .Produces<InventoryItemResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapDelete(InventoryApiRoutes.ItemById, DeleteItemAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteInventoryItem")
            .WithSummary("Deletes an unreferenced Inventory item and its attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        items.MapPost(InventoryApiRoutes.ItemStockAdjustments, AdjustStockAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("AdjustInventoryItemStock")
            .WithSummary("Applies a quick stock increase or decrease to an accessible item")
            .Produces<InventoryItemResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapGet(InventoryApiRoutes.ItemAttachments, ListItemAttachmentsAsync)
            .WithName("ListInventoryItemAttachments")
            .WithSummary("Lists the attachments of an accessible Inventory item")
            .Produces<IReadOnlyList<InventoryAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapPost(InventoryApiRoutes.ItemAttachments, UploadItemAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadInventoryItemAttachment")
            .WithSummary("Uploads one attachment for an accessible Inventory item")
            .Produces<InventoryAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapGet(InventoryApiRoutes.ItemAttachmentById, DownloadItemAttachmentAsync)
            .WithName("DownloadInventoryItemAttachment")
            .WithSummary("Downloads one attachment of an accessible Inventory item")
            .ProducesProblem(StatusCodes.Status404NotFound);

        items.MapDelete(InventoryApiRoutes.ItemAttachmentById, DeleteItemAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteInventoryItemAttachment")
            .WithSummary("Removes one attachment of an accessible Inventory item")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/categories", ListCategoriesAsync)
            .WithName("ListInventoryCategories")
            .WithSummary("Returns the Inventory category catalog")
            .Produces<IReadOnlyList<InventoryCategoryResponse>>();

        var categories = group.MapGroup("/categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateInventoryCategory").WithSummary("Creates a category at the end of the catalog").Produces<InventoryCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(InventoryApiRoutes.CategoryById, UpdateCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateInventoryCategory").WithSummary("Updates an Inventory category").Produces<InventoryCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(InventoryApiRoutes.CategoryMove, MoveCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveInventoryCategory").WithSummary("Moves an Inventory category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(InventoryApiRoutes.CategoryDeletionImpact, CategoryImpactAsync).WithName("GetInventoryCategoryDeletionImpact").WithSummary("Returns privacy-neutral category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(InventoryApiRoutes.CategoryById, DeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteInventoryCategory").WithSummary("Deletes an unreferenced Inventory category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(InventoryApiRoutes.CategoryReplaceAndDelete, ReplaceAndDeleteCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteInventoryCategory").WithSummary("Migrates references and deletes an Inventory category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapOrderEndpoints(RouteGroupBuilder group)
    {
        var orders = group.MapGroup("/orders");

        orders.MapGet("", ListOrdersAsync)
            .WithName("ListInventoryOrders")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible Inventory orders")
            .Produces<PaginatedResponse<InventoryOrderSummaryResponse>>();

        orders.MapGet(InventoryApiRoutes.OrderById, GetOrderAsync)
            .WithName("GetInventoryOrder")
            .WithSummary("Returns the detail of an accessible Inventory order with its lines and attachments")
            .Produces<InventoryOrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("", CreateOrderAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateInventoryOrder")
            .WithSummary("Creates an Inventory supplier order with its full line set")
            .Produces<InventoryOrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        orders.MapPut(InventoryApiRoutes.OrderById, UpdateOrderAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateInventoryOrder")
            .WithSummary("Replaces an accessible Inventory order and its full line set")
            .Produces<InventoryOrderResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapDelete(InventoryApiRoutes.OrderById, DeleteOrderAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteInventoryOrder")
            .WithSummary("Deletes an accessible Inventory order and its attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost(InventoryApiRoutes.OrderReceive, ReceiveOrderAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReceiveInventoryOrder")
            .WithSummary("Receives an active Inventory order and increases each referenced item's stock")
            .Produces<InventoryOrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapGet(InventoryApiRoutes.OrderAttachments, ListOrderAttachmentsAsync)
            .WithName("ListInventoryOrderAttachments")
            .WithSummary("Lists the attachments of an accessible Inventory order")
            .Produces<IReadOnlyList<InventoryAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost(InventoryApiRoutes.OrderAttachments, UploadOrderAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadInventoryOrderAttachment")
            .WithSummary("Uploads one attachment for an accessible Inventory order")
            .Produces<InventoryAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapGet(InventoryApiRoutes.OrderAttachmentById, DownloadOrderAttachmentAsync)
            .WithName("DownloadInventoryOrderAttachment")
            .WithSummary("Downloads one attachment of an accessible Inventory order")
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapDelete(InventoryApiRoutes.OrderAttachmentById, DeleteOrderAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteInventoryOrderAttachment")
            .WithSummary("Removes one attachment of an accessible Inventory order")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapLocationEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/locations", ListLocationsAsync)
            .WithName("ListInventoryLocations")
            .WithSummary("Returns the Inventory location catalog")
            .Produces<IReadOnlyList<InventoryLocationResponse>>();

        var locations = group.MapGroup("/locations").RequireAuthorization(IdentityPolicies.Admin);
        locations.MapPost("", CreateLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateInventoryLocation").WithSummary("Creates a location at the end of the catalog").Produces<InventoryLocationResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPut(InventoryApiRoutes.LocationById, UpdateLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateInventoryLocation").WithSummary("Updates an Inventory location").Produces<InventoryLocationResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPost(InventoryApiRoutes.LocationMove, MoveLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveInventoryLocation").WithSummary("Moves an Inventory location one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        locations.MapGet(InventoryApiRoutes.LocationDeletionImpact, LocationImpactAsync).WithName("GetInventoryLocationDeletionImpact").WithSummary("Returns privacy-neutral location deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        locations.MapDelete(InventoryApiRoutes.LocationById, DeleteLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteInventoryLocation").WithSummary("Deletes an unreferenced Inventory location").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        locations.MapPost(InventoryApiRoutes.LocationReplaceAndDelete, ReplaceAndDeleteLocationAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteInventoryLocation").WithSummary("Migrates references and deletes an Inventory location atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListItemsAsync(
        [AsParameters] InventoryItemListQuery query,
        InventoryReadService read,
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

        var result = await read.ListItemsAsync(filter, pagination, sort, userId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetItemAsync(
        int itemId,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var item = await read.GetItemAsync(itemId, userId, cancellationToken);
        if (item is null)
        {
            throw InventoryItemProblem.NotFound();
        }

        return TypedResults.Ok(item);
    }

    private static async Task<IResult> CreateItemAsync(
        CreateInventoryItemRequest request,
        InventoryItemWriteService write,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int itemId;
        try
        {
            itemId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (InventoryValidationException exception)
        {
            throw InventoryItemProblem.From(exception);
        }

        var created = await read.GetItemAsync(itemId, userId, cancellationToken);
        return TypedResults.Created($"/api/inventory/items/{itemId}", created);
    }

    private static async Task<IResult> UpdateItemAsync(
        int itemId,
        UpdateInventoryItemRequest request,
        InventoryItemWriteService write,
        InventoryReadService read,
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
            updated = await write.UpdateAsync(itemId, request, userId, cancellationToken);
        }
        catch (InventoryValidationException exception)
        {
            throw InventoryItemProblem.From(exception);
        }

        if (!updated)
        {
            throw InventoryItemProblem.NotFound();
        }

        var item = await read.GetItemAsync(itemId, userId, cancellationToken);
        return TypedResults.Ok(item);
    }

    private static async Task<IResult> DeleteItemAsync(
        int itemId,
        InventoryItemWriteService write,
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
            deleted = await write.DeleteAsync(itemId, userId, cancellationToken);
        }
        catch (InventoryValidationException exception)
        {
            throw InventoryItemProblem.From(exception);
        }

        if (!deleted)
        {
            throw InventoryItemProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> AdjustStockAsync(
        int itemId,
        InventoryStockAdjustmentRequest request,
        InventoryItemWriteService write,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool adjusted;
        try
        {
            adjusted = await write.AdjustStockAsync(itemId, request, userId, cancellationToken);
        }
        catch (InventoryValidationException exception)
        {
            throw InventoryItemProblem.From(exception);
        }

        if (!adjusted)
        {
            throw InventoryItemProblem.NotFound();
        }

        var item = await read.GetItemAsync(itemId, userId, cancellationToken);
        return TypedResults.Ok(item);
    }

    private static async Task<IResult> ListOrdersAsync(
        [AsParameters] InventoryOrderListQuery query,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListOrdersAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetOrderAsync(
        int orderId,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var order = await read.GetOrderAsync(orderId, userId, cancellationToken);
        if (order is null)
        {
            throw InventoryOrderProblem.NotFound();
        }

        return TypedResults.Ok(order);
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateInventoryOrderRequest request,
        InventoryOrderWriteService write,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int orderId;
        try
        {
            orderId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (InventoryOrderValidationException exception)
        {
            throw InventoryOrderProblem.From(exception);
        }

        var created = await read.GetOrderAsync(orderId, userId, cancellationToken);
        return TypedResults.Created($"/api/inventory/orders/{orderId}", created);
    }

    private static async Task<IResult> UpdateOrderAsync(
        int orderId,
        UpdateInventoryOrderRequest request,
        InventoryOrderWriteService write,
        InventoryReadService read,
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
            updated = await write.UpdateAsync(orderId, request, userId, cancellationToken);
        }
        catch (InventoryOrderValidationException exception)
        {
            throw InventoryOrderProblem.From(exception);
        }

        if (!updated)
        {
            throw InventoryOrderProblem.NotFound();
        }

        var order = await read.GetOrderAsync(orderId, userId, cancellationToken);
        return TypedResults.Ok(order);
    }

    private static async Task<IResult> DeleteOrderAsync(
        int orderId,
        InventoryOrderWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(orderId, userId, cancellationToken);
        if (!deleted)
        {
            throw InventoryOrderProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReceiveOrderAsync(
        int orderId,
        InventoryOrderWriteService write,
        InventoryReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        bool received;
        try
        {
            received = await write.ReceiveAsync(orderId, userId, cancellationToken);
        }
        catch (InventoryOrderValidationException exception)
        {
            throw InventoryOrderProblem.From(exception);
        }

        if (!received)
        {
            throw InventoryOrderProblem.NotFound();
        }

        var order = await read.GetOrderAsync(orderId, userId, cancellationToken);
        return TypedResults.Ok(order);
    }

    private static async Task<IResult> ListItemAttachmentsAsync(
        int itemId,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ItemAccessibleAsync(itemId, userId, cancellationToken))
        {
            throw InventoryItemProblem.NotFound();
        }

        var descriptors = await attachments.ListByOwnerAsync(InventoryAttachments.ItemOwner(itemId), cancellationToken);
        return TypedResults.Ok(descriptors.Select(ToAttachment).ToArray());
    }

    private static async Task<IResult> UploadItemAttachmentAsync(
        int itemId,
        HttpRequest request,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ItemAccessibleAsync(itemId, userId, cancellationToken))
        {
            throw InventoryItemProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw InventoryItemProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw InventoryItemProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        await using var stream = file.OpenReadStream();
        AttachmentDescriptor created;
        try
        {
            created = await attachments.CreateAsync(
                new(InventoryAttachments.ItemOwner(itemId), file.FileName, file.ContentType, stream),
                userId,
                cancellationToken);
        }
        catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            throw InventoryItemProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
        }

        return TypedResults.Created(
            $"/api/inventory/items/{itemId}/attachments/{created.Id.Value}",
            ToAttachment(created));
    }

    private static async Task<IResult> DownloadItemAttachmentAsync(
        int itemId,
        int attachmentId,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ItemAccessibleAsync(itemId, userId, cancellationToken))
        {
            throw InventoryItemProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            InventoryAttachments.ItemOwner(itemId),
            cancellationToken);
        if (download is null)
        {
            throw InventoryItemProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteItemAttachmentAsync(
        int itemId,
        int attachmentId,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ItemAccessibleAsync(itemId, userId, cancellationToken))
        {
            throw InventoryItemProblem.NotFound();
        }

        var removed = await attachments.DeleteAsync(
            new(attachmentId),
            InventoryAttachments.ItemOwner(itemId),
            cancellationToken);
        if (!removed)
        {
            throw InventoryItemProblem.AttachmentNotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListOrderAttachmentsAsync(
        int orderId,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.OrderAccessibleAsync(orderId, userId, cancellationToken))
        {
            throw InventoryOrderProblem.NotFound();
        }

        var descriptors = await attachments.ListByOwnerAsync(InventoryAttachments.OrderOwner(orderId), cancellationToken);
        return TypedResults.Ok(descriptors.Select(ToAttachment).ToArray());
    }

    private static async Task<IResult> UploadOrderAttachmentAsync(
        int orderId,
        HttpRequest request,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.OrderAccessibleAsync(orderId, userId, cancellationToken))
        {
            throw InventoryOrderProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw InventoryOrderProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw InventoryOrderProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        await using var stream = file.OpenReadStream();
        AttachmentDescriptor created;
        try
        {
            created = await attachments.CreateAsync(
                new(InventoryAttachments.OrderOwner(orderId), file.FileName, file.ContentType, stream),
                userId,
                cancellationToken);
        }
        catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
        {
            throw InventoryOrderProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
        }

        return TypedResults.Created(
            $"/api/inventory/orders/{orderId}/attachments/{created.Id.Value}",
            ToAttachment(created));
    }

    private static async Task<IResult> DownloadOrderAttachmentAsync(
        int orderId,
        int attachmentId,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.OrderAccessibleAsync(orderId, userId, cancellationToken))
        {
            throw InventoryOrderProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            InventoryAttachments.OrderOwner(orderId),
            cancellationToken);
        if (download is null)
        {
            throw InventoryOrderProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteOrderAttachmentAsync(
        int orderId,
        int attachmentId,
        InventoryReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.OrderAccessibleAsync(orderId, userId, cancellationToken))
        {
            throw InventoryOrderProblem.NotFound();
        }

        var removed = await attachments.DeleteAsync(
            new(attachmentId),
            InventoryAttachments.OrderOwner(orderId),
            cancellationToken);
        if (!removed)
        {
            throw InventoryOrderProblem.AttachmentNotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListCategoriesAsync(InventoryReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListLocationsAsync(InventoryReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListLocationsAsync(cancellationToken));

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw InventoryCategoryProblem.NotFound();

    private static CatalogMoveDirection CategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw InventoryCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection LocationDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw InventoryLocationProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateCategoryAsync(CatalogItemRequest request, InventoryCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/inventory/categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateCategoryAsync(int categoryId, CatalogItemRequest request, InventoryCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveCategoryAsync(int categoryId, CatalogMoveRequest request, InventoryCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, CategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CategoryImpactAsync(int categoryId, InventoryCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteCategoryAsync(int categoryId, InventoryCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteCategoryAsync(int categoryId, CatalogReplacementRequest request, InventoryCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateLocationAsync(CatalogItemRequest request, InventoryLocationManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/inventory/locations/{value.Id}", value);
    }

    private static async Task<IResult> UpdateLocationAsync(int locationId, CatalogItemRequest request, InventoryLocationManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(locationId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveLocationAsync(int locationId, CatalogMoveRequest request, InventoryLocationManagementService service, CancellationToken token)
    {
        await service.MoveAsync(locationId, LocationDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> LocationImpactAsync(int locationId, InventoryLocationManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(locationId, token));

    private static async Task<IResult> DeleteLocationAsync(int locationId, InventoryLocationManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(locationId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteLocationAsync(int locationId, CatalogReplacementRequest request, InventoryLocationManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(locationId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }

    private static InventoryAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt);
}
