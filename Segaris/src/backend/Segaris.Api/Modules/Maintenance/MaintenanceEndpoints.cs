using System.Globalization;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Api.Modules.Maintenance.Mutations;
using Segaris.Api.Modules.Maintenance.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>
/// Maps the Maintenance HTTP surface. State-changing routes carry antiforgery
/// protection and never expose EF Core entities.
/// </summary>
internal static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("maintenance", MaintenanceApiRoutes.Tag)
            .RequireAuthorization();

        MapTaskEndpoints(group);
        MapTypeEndpoints(group);

        return endpoints;
    }

    private static void MapTaskEndpoints(RouteGroupBuilder group)
    {
        var tasks = group.MapGroup("/tasks");
        tasks.MapGet("", ListTasksAsync)
            .WithName("ListMaintenanceTasks")
            .WithSummary("Returns a paginated, filtered, and sorted table of accessible Maintenance tasks")
            .Produces<PaginatedResponse<MaintenanceTaskSummaryResponse>>();
        tasks.MapPost("", CreateTaskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateMaintenanceTask")
            .WithSummary("Creates a Maintenance task")
            .Produces<MaintenanceTaskResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        tasks.MapGet(MaintenanceApiRoutes.TaskById, GetTaskAsync)
            .WithName("GetMaintenanceTask")
            .WithSummary("Returns the detail of an accessible Maintenance task")
            .Produces<MaintenanceTaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        tasks.MapPut(MaintenanceApiRoutes.TaskById, UpdateTaskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateMaintenanceTask")
            .WithSummary("Replaces an accessible Maintenance task")
            .Produces<MaintenanceTaskResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        tasks.MapDelete(MaintenanceApiRoutes.TaskById, DeleteTaskAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteMaintenanceTask")
            .WithSummary("Deletes an accessible Maintenance task")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        tasks.MapGet(MaintenanceApiRoutes.TaskAttachments, ListAttachmentsAsync)
            .WithName("ListMaintenanceTaskAttachments")
            .WithSummary("Lists the attachments of an accessible Maintenance task")
            .Produces<IReadOnlyList<MaintenanceTaskAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        tasks.MapPost(MaintenanceApiRoutes.TaskAttachments, UploadAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadMaintenanceTaskAttachment")
            .WithSummary("Uploads one attachment for an accessible Maintenance task")
            .Produces<MaintenanceTaskAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        tasks.MapGet(MaintenanceApiRoutes.TaskAttachmentById, DownloadAttachmentAsync)
            .WithName("DownloadMaintenanceTaskAttachment")
            .WithSummary("Downloads one attachment of an accessible Maintenance task")
            .ProducesProblem(StatusCodes.Status404NotFound);
        tasks.MapDelete(MaintenanceApiRoutes.TaskAttachmentById, DeleteAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteMaintenanceTaskAttachment")
            .WithSummary("Removes one attachment of an accessible Maintenance task")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapTypeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/types", ListTypesAsync)
            .WithName("ListMaintenanceTypes")
            .WithSummary("Returns the Maintenance type catalogue")
            .Produces<IReadOnlyList<MaintenanceTypeResponse>>();

        var types = group.MapGroup("/types").RequireAuthorization(IdentityPolicies.Admin);
        types.MapPost("", CreateTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateMaintenanceType").WithSummary("Creates a maintenance type at the end of the catalogue").Produces<MaintenanceTypeResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        types.MapPut(MaintenanceApiRoutes.TypeById, UpdateTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateMaintenanceType").WithSummary("Updates a maintenance type").Produces<MaintenanceTypeResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        types.MapPost(MaintenanceApiRoutes.TypeMove, MoveTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveMaintenanceType").WithSummary("Moves a maintenance type one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        types.MapGet(MaintenanceApiRoutes.TypeDeletionImpact, TypeImpactAsync).WithName("GetMaintenanceTypeDeletionImpact").WithSummary("Returns privacy-neutral maintenance type deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        types.MapDelete(MaintenanceApiRoutes.TypeById, DeleteTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteMaintenanceType").WithSummary("Deletes an unreferenced maintenance type").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        types.MapPost(MaintenanceApiRoutes.TypeReplaceAndDelete, ReplaceAndDeleteTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteMaintenanceType").WithSummary("Migrates references and deletes a maintenance type atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListTypesAsync(MaintenanceTypeReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListAsync(cancellationToken));

    private static async Task<IResult> ListTasksAsync(
        [AsParameters] MaintenanceTaskListQuery query,
        MaintenanceTaskReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListTasksAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetTaskAsync(
        int taskId,
        MaintenanceTaskReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var task = await read.GetTaskAsync(taskId, userId, cancellationToken);
        if (task is null)
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        return TypedResults.Ok(task);
    }

    private static async Task<IResult> CreateTaskAsync(
        CreateMaintenanceTaskRequest request,
        MaintenanceTaskWriteService write,
        MaintenanceTaskReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int taskId;
        try
        {
            taskId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (MaintenanceValidationException exception)
        {
            throw MaintenanceTaskProblem.From(exception);
        }

        var created = await read.GetTaskAsync(taskId, userId, cancellationToken);
        return TypedResults.Created($"/api/maintenance/tasks/{taskId}", created);
    }

    private static async Task<IResult> UpdateTaskAsync(
        int taskId,
        UpdateMaintenanceTaskRequest request,
        MaintenanceTaskWriteService write,
        MaintenanceTaskReadService read,
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
            updated = await write.UpdateAsync(taskId, request, userId, cancellationToken);
        }
        catch (MaintenanceValidationException exception)
        {
            throw MaintenanceTaskProblem.From(exception);
        }

        if (!updated)
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        var task = await read.GetTaskAsync(taskId, userId, cancellationToken);
        return TypedResults.Ok(task);
    }

    private static async Task<IResult> DeleteTaskAsync(
        int taskId,
        MaintenanceTaskWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(taskId, userId, cancellationToken);
        if (!deleted)
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListAttachmentsAsync(
        int taskId,
        MaintenanceTaskReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TaskAccessibleAsync(taskId, userId, cancellationToken))
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        var descriptors = await attachments.ListByOwnerAsync(MaintenanceAttachments.TaskOwner(taskId), cancellationToken);
        return TypedResults.Ok(descriptors.Select(ToAttachment).ToArray());
    }

    private static async Task<IResult> UploadAttachmentAsync(
        int taskId,
        HttpRequest request,
        MaintenanceTaskReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TaskAccessibleAsync(taskId, userId, cancellationToken))
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw MaintenanceTaskProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw MaintenanceTaskProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        AttachmentDescriptor created;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                created = await attachments.CreateAsync(
                    new(MaintenanceAttachments.TaskOwner(taskId), file.FileName, file.ContentType, stream),
                    userId,
                    cancellationToken);
            }
            catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
            {
                throw MaintenanceTaskProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
            }
        }

        return TypedResults.Created(
            $"/api/maintenance/tasks/{taskId}/attachments/{created.Id.Value}",
            ToAttachment(created));
    }

    private static async Task<IResult> DownloadAttachmentAsync(
        int taskId,
        int attachmentId,
        MaintenanceTaskReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TaskAccessibleAsync(taskId, userId, cancellationToken))
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            MaintenanceAttachments.TaskOwner(taskId),
            cancellationToken);
        if (download is null)
        {
            throw MaintenanceTaskProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteAttachmentAsync(
        int taskId,
        int attachmentId,
        MaintenanceTaskReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TaskAccessibleAsync(taskId, userId, cancellationToken))
        {
            throw MaintenanceTaskProblem.NotFound();
        }

        var removed = await attachments.DeleteAsync(
            new(attachmentId),
            MaintenanceAttachments.TaskOwner(taskId),
            cancellationToken);
        if (!removed)
        {
            throw MaintenanceTaskProblem.AttachmentNotFound();
        }

        return TypedResults.NoContent();
    }

    private static MaintenanceTaskAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt);

    private static UserId CatalogActor(ICurrentUser currentUser) => currentUser.UserId ?? throw MaintenanceTypeProblem.NotFound();

    private static CatalogMoveDirection TypeDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw MaintenanceTypeProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateTypeAsync(CatalogItemRequest request, MaintenanceTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, CatalogActor(user), token);
        return TypedResults.Created($"/api/maintenance/types/{value.Id}", value);
    }

    private static async Task<IResult> UpdateTypeAsync(int typeId, CatalogItemRequest request, MaintenanceTypeManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(typeId, request, CatalogActor(user), token));

    private static async Task<IResult> MoveTypeAsync(int typeId, CatalogMoveRequest request, MaintenanceTypeManagementService service, CancellationToken token)
    {
        await service.MoveAsync(typeId, TypeDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> TypeImpactAsync(int typeId, MaintenanceTypeManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(typeId, token));

    private static async Task<IResult> DeleteTypeAsync(int typeId, MaintenanceTypeManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(typeId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteTypeAsync(int typeId, CatalogReplacementRequest request, MaintenanceTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(typeId, request, CatalogActor(user), token);
        return TypedResults.NoContent();
    }
}
