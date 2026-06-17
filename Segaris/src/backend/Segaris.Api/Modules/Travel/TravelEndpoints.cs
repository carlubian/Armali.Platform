using System.Globalization;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Api.Modules.Travel.Mutations;
using Segaris.Api.Modules.Travel.Queries;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel;

/// <summary>
/// Maps the Travel HTTP surface. Wave 1 exposes the module-owned trip-type and
/// expense-category catalog reads and the administrator-only catalog management
/// routes surfaced through Configuration; later waves add the trip read, mutation,
/// itinerary, attachment, and expense sub-resource routes frozen in
/// <see cref="TravelApiRoutes"/>. State-changing routes carry antiforgery protection
/// and never expose EF Core entities.
/// </summary>
internal static class TravelEndpoints
{
    public static IEndpointRouteBuilder MapTravelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("travel", TravelApiRoutes.Tag)
            .RequireAuthorization();

        MapTripEndpoints(group);
        MapTripTypeEndpoints(group);
        MapExpenseCategoryEndpoints(group);

        return endpoints;
    }

    private static void MapTripEndpoints(RouteGroupBuilder group)
    {
        var trips = group.MapGroup("/trips");

        trips.MapGet("", ListTripsAsync)
            .WithName("ListTravelTrips")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible Travel trips")
            .Produces<PaginatedResponse<TravelTripSummaryResponse>>();

        trips.MapGet(TravelApiRoutes.TripById, GetTripAsync)
            .WithName("GetTravelTrip")
            .WithSummary("Returns the detail of an accessible Travel trip with itinerary and per-currency totals")
            .Produces<TravelTripResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapPost("", CreateTripAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateTravelTrip")
            .WithSummary("Creates a Travel trip with its embedded itinerary")
            .Produces<TravelTripResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        trips.MapPut(TravelApiRoutes.TripById, UpdateTripAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateTravelTrip")
            .WithSummary("Updates a Travel trip and fully replaces its embedded itinerary")
            .Produces<TravelTripResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapDelete(TravelApiRoutes.TripById, DeleteTripAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteTravelTrip")
            .WithSummary("Physically deletes a Travel trip, itinerary, expenses, and owned attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapGet(TravelApiRoutes.TripAttachments, ListTripAttachmentsAsync)
            .WithName("ListTravelTripAttachments")
            .WithSummary("Lists the attachments of an accessible Travel trip")
            .Produces<IReadOnlyList<TravelAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapPost(TravelApiRoutes.TripAttachments, UploadTripAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadTravelTripAttachment")
            .WithSummary("Uploads one attachment for an accessible Travel trip")
            .Produces<TravelAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapGet(TravelApiRoutes.TripAttachmentById, DownloadTripAttachmentAsync)
            .WithName("DownloadTravelTripAttachment")
            .WithSummary("Downloads one attachment of an accessible Travel trip")
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapDelete(TravelApiRoutes.TripAttachmentById, DeleteTripAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteTravelTripAttachment")
            .WithSummary("Removes one attachment of an accessible Travel trip")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapGet(TravelApiRoutes.TripExpenses, ListExpensesAsync)
            .WithName("ListTravelExpenses")
            .WithSummary("Returns a paginated, filtered, and sorted expense list for an accessible Travel trip")
            .Produces<PaginatedResponse<TravelExpenseSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapPost(TravelApiRoutes.TripExpenses, CreateExpenseAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateTravelExpense")
            .WithSummary("Creates an expense under an accessible Travel trip")
            .Produces<TravelExpenseResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapGet(TravelApiRoutes.TripExpenseById, GetExpenseAsync)
            .WithName("GetTravelExpense")
            .WithSummary("Returns one expense from an accessible Travel trip")
            .Produces<TravelExpenseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapPut(TravelApiRoutes.TripExpenseById, UpdateExpenseAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateTravelExpense")
            .WithSummary("Updates one expense under an accessible Travel trip")
            .Produces<TravelExpenseResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapDelete(TravelApiRoutes.TripExpenseById, DeleteExpenseAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteTravelExpense")
            .WithSummary("Deletes one expense and its attachments")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapGet(TravelApiRoutes.TripExpenseAttachments, ListExpenseAttachmentsAsync)
            .WithName("ListTravelExpenseAttachments")
            .WithSummary("Lists the attachments of an accessible Travel expense")
            .Produces<IReadOnlyList<TravelAttachmentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapPost(TravelApiRoutes.TripExpenseAttachments, UploadExpenseAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithName("UploadTravelExpenseAttachment")
            .WithSummary("Uploads one attachment for an accessible Travel expense")
            .Produces<TravelAttachmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapGet(TravelApiRoutes.TripExpenseAttachmentById, DownloadExpenseAttachmentAsync)
            .WithName("DownloadTravelExpenseAttachment")
            .WithSummary("Downloads one attachment of an accessible Travel expense")
            .ProducesProblem(StatusCodes.Status404NotFound);

        trips.MapDelete(TravelApiRoutes.TripExpenseAttachmentById, DeleteExpenseAttachmentAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteTravelExpenseAttachment")
            .WithSummary("Removes one attachment of an accessible Travel expense")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapTripTypeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/trip-types", ListTripTypesAsync)
            .WithName("ListTravelTripTypes")
            .WithSummary("Returns the Travel trip type catalog")
            .Produces<IReadOnlyList<TravelTripTypeResponse>>();

        var tripTypes = group.MapGroup("/trip-types").RequireAuthorization(IdentityPolicies.Admin);
        tripTypes.MapPost("", CreateTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateTravelTripType").WithSummary("Creates a trip type at the end of the catalog").Produces<TravelTripTypeResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        tripTypes.MapPut(TravelApiRoutes.TripTypeById, UpdateTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateTravelTripType").WithSummary("Updates a Travel trip type").Produces<TravelTripTypeResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        tripTypes.MapPost(TravelApiRoutes.TripTypeMove, MoveTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveTravelTripType").WithSummary("Moves a Travel trip type one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        tripTypes.MapGet(TravelApiRoutes.TripTypeDeletionImpact, TripTypeImpactAsync).WithName("GetTravelTripTypeDeletionImpact").WithSummary("Returns privacy-neutral trip type deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        tripTypes.MapDelete(TravelApiRoutes.TripTypeById, DeleteTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteTravelTripType").WithSummary("Deletes an unreferenced Travel trip type").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        tripTypes.MapPost(TravelApiRoutes.TripTypeReplaceAndDelete, ReplaceAndDeleteTripTypeAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteTravelTripType").WithSummary("Migrates references and deletes a Travel trip type atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapExpenseCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/expense-categories", ListExpenseCategoriesAsync)
            .WithName("ListTravelExpenseCategories")
            .WithSummary("Returns the Travel expense category catalog")
            .Produces<IReadOnlyList<TravelExpenseCategoryResponse>>();

        var categories = group.MapGroup("/expense-categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateTravelExpenseCategory").WithSummary("Creates an expense category at the end of the catalog").Produces<TravelExpenseCategoryResponse>(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(TravelApiRoutes.ExpenseCategoryById, UpdateExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateTravelExpenseCategory").WithSummary("Updates a Travel expense category").Produces<TravelExpenseCategoryResponse>().ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(TravelApiRoutes.ExpenseCategoryMove, MoveExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveTravelExpenseCategory").WithSummary("Moves a Travel expense category one position").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(TravelApiRoutes.ExpenseCategoryDeletionImpact, ExpenseCategoryImpactAsync).WithName("GetTravelExpenseCategoryDeletionImpact").WithSummary("Returns privacy-neutral expense category deletion impact").Produces<CatalogDeletionImpactResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(TravelApiRoutes.ExpenseCategoryById, DeleteExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteTravelExpenseCategory").WithSummary("Deletes an unreferenced Travel expense category").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(TravelApiRoutes.ExpenseCategoryReplaceAndDelete, ReplaceAndDeleteExpenseCategoryAsync).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteTravelExpenseCategory").WithSummary("Migrates references and deletes a Travel expense category atomically").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListTripTypesAsync(TravelReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListTripTypesAsync(cancellationToken));

    private static async Task<IResult> ListExpenseCategoriesAsync(TravelReadService read, CancellationToken cancellationToken) =>
        TypedResults.Ok(await read.ListExpenseCategoriesAsync(cancellationToken));

    private static async Task<IResult> ListTripsAsync(
        [AsParameters] TravelTripListQuery query,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListTripsAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetTripAsync(
        int tripId,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var trip = await read.GetTripAsync(tripId, userId, cancellationToken);
        if (trip is null)
        {
            throw TravelTripProblem.NotFound();
        }

        return TypedResults.Ok(trip);
    }

    private static async Task<IResult> CreateTripAsync(
        CreateTravelTripRequest request,
        TravelTripWriteService write,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int tripId;
        try
        {
            tripId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (TravelValidationException exception)
        {
            throw TravelTripProblem.From(exception);
        }

        var created = await read.GetTripAsync(tripId, userId, cancellationToken);
        return TypedResults.Created($"/api/travel/trips/{tripId}", created);
    }

    private static async Task<IResult> UpdateTripAsync(
        int tripId,
        UpdateTravelTripRequest request,
        TravelTripWriteService write,
        TravelReadService read,
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
            updated = await write.UpdateAsync(tripId, request, userId, cancellationToken);
        }
        catch (TravelValidationException exception)
        {
            throw TravelTripProblem.From(exception);
        }

        if (!updated)
        {
            throw TravelTripProblem.NotFound();
        }

        var trip = await read.GetTripAsync(tripId, userId, cancellationToken);
        return TypedResults.Ok(trip);
    }

    private static async Task<IResult> DeleteTripAsync(
        int tripId,
        TravelTripWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(tripId, userId, cancellationToken);
        if (!deleted)
        {
            throw TravelTripProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListTripAttachmentsAsync(
        int tripId,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TripAccessibleAsync(tripId, userId, cancellationToken))
        {
            throw TravelTripProblem.NotFound();
        }

        var descriptors = await attachments.ListByOwnerAsync(TravelAttachments.TripOwner(tripId), cancellationToken);
        return TypedResults.Ok(descriptors.Select(ToAttachment).ToArray());
    }

    private static async Task<IResult> UploadTripAttachmentAsync(
        int tripId,
        HttpRequest request,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TripAccessibleAsync(tripId, userId, cancellationToken))
        {
            throw TravelTripProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw TravelTripProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw TravelTripProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        AttachmentDescriptor created;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                created = await attachments.CreateAsync(
                    new(TravelAttachments.TripOwner(tripId), file.FileName, file.ContentType, stream),
                    userId,
                    cancellationToken);
            }
            catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
            {
                throw TravelTripProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
            }
        }

        return TypedResults.Created(
            $"/api/travel/trips/{tripId}/attachments/{created.Id.Value}",
            ToAttachment(created));
    }

    private static async Task<IResult> DownloadTripAttachmentAsync(
        int tripId,
        int attachmentId,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TripAccessibleAsync(tripId, userId, cancellationToken))
        {
            throw TravelTripProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            TravelAttachments.TripOwner(tripId),
            cancellationToken);
        if (download is null)
        {
            throw TravelTripProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteTripAttachmentAsync(
        int tripId,
        int attachmentId,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.TripAccessibleAsync(tripId, userId, cancellationToken))
        {
            throw TravelTripProblem.NotFound();
        }

        var removed = await attachments.DeleteAsync(
            new(attachmentId),
            TravelAttachments.TripOwner(tripId),
            cancellationToken);
        if (!removed)
        {
            throw TravelTripProblem.AttachmentNotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListExpensesAsync(
        int tripId,
        [AsParameters] TravelExpenseListQuery query,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListExpensesAsync(
            tripId,
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        if (result is null)
        {
            throw TravelTripProblem.NotFound();
        }

        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetExpenseAsync(
        int tripId,
        int expenseId,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var expense = await read.GetExpenseAsync(tripId, expenseId, userId, cancellationToken);
        if (expense is null)
        {
            throw TravelExpenseProblem.NotFound();
        }

        return TypedResults.Ok(expense);
    }

    private static async Task<IResult> CreateExpenseAsync(
        int tripId,
        CreateTravelExpenseRequest request,
        TravelExpenseWriteService write,
        TravelReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int? expenseId;
        try
        {
            expenseId = await write.CreateAsync(tripId, request, userId, cancellationToken);
        }
        catch (TravelValidationException exception)
        {
            throw TravelExpenseProblem.From(exception);
        }

        if (expenseId is null)
        {
            throw TravelTripProblem.NotFound();
        }

        var created = await read.GetExpenseAsync(tripId, expenseId.Value, userId, cancellationToken);
        return TypedResults.Created($"/api/travel/trips/{tripId}/expenses/{expenseId.Value}", created);
    }

    private static async Task<IResult> UpdateExpenseAsync(
        int tripId,
        int expenseId,
        UpdateTravelExpenseRequest request,
        TravelExpenseWriteService write,
        TravelReadService read,
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
            updated = await write.UpdateAsync(tripId, expenseId, request, userId, cancellationToken);
        }
        catch (TravelValidationException exception)
        {
            throw TravelExpenseProblem.From(exception);
        }

        if (!updated)
        {
            throw TravelExpenseProblem.NotFound();
        }

        var expense = await read.GetExpenseAsync(tripId, expenseId, userId, cancellationToken);
        return TypedResults.Ok(expense);
    }

    private static async Task<IResult> DeleteExpenseAsync(
        int tripId,
        int expenseId,
        TravelExpenseWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(tripId, expenseId, userId, cancellationToken);
        if (!deleted)
        {
            throw TravelExpenseProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListExpenseAttachmentsAsync(
        int tripId,
        int expenseId,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ExpenseAccessibleAsync(tripId, expenseId, userId, cancellationToken))
        {
            throw TravelExpenseProblem.NotFound();
        }

        var descriptors = await attachments.ListByOwnerAsync(TravelAttachments.ExpenseOwner(expenseId), cancellationToken);
        return TypedResults.Ok(descriptors.Select(ToAttachment).ToArray());
    }

    private static async Task<IResult> UploadExpenseAttachmentAsync(
        int tripId,
        int expenseId,
        HttpRequest request,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ExpenseAccessibleAsync(tripId, expenseId, userId, cancellationToken))
        {
            throw TravelExpenseProblem.NotFound();
        }

        if (!request.HasFormContentType)
        {
            throw TravelTripProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw TravelTripProblem.AttachmentInvalid("file", "A multipart form file is required.");
        }

        AttachmentDescriptor created;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                created = await attachments.CreateAsync(
                    new(TravelAttachments.ExpenseOwner(expenseId), file.FileName, file.ContentType, stream),
                    userId,
                    cancellationToken);
            }
            catch (ApiProblemException exception) when (exception.StatusCode == StatusCodes.Status400BadRequest)
            {
                throw TravelTripProblem.AttachmentInvalid("file", exception.Message, exception.Errors);
            }
        }

        return TypedResults.Created(
            $"/api/travel/trips/{tripId}/expenses/{expenseId}/attachments/{created.Id.Value}",
            ToAttachment(created));
    }

    private static async Task<IResult> DownloadExpenseAttachmentAsync(
        int tripId,
        int expenseId,
        int attachmentId,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ExpenseAccessibleAsync(tripId, expenseId, userId, cancellationToken))
        {
            throw TravelExpenseProblem.NotFound();
        }

        var download = await attachments.OpenReadAsync(
            new(attachmentId),
            TravelAttachments.ExpenseOwner(expenseId),
            cancellationToken);
        if (download is null)
        {
            throw TravelTripProblem.AttachmentNotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteExpenseAttachmentAsync(
        int tripId,
        int expenseId,
        int attachmentId,
        TravelReadService read,
        IAttachmentService attachments,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        if (!await read.ExpenseAccessibleAsync(tripId, expenseId, userId, cancellationToken))
        {
            throw TravelExpenseProblem.NotFound();
        }

        var removed = await attachments.DeleteAsync(
            new(attachmentId),
            TravelAttachments.ExpenseOwner(expenseId),
            cancellationToken);
        if (!removed)
        {
            throw TravelTripProblem.AttachmentNotFound();
        }

        return TypedResults.NoContent();
    }

    private static TravelAttachmentResponse ToAttachment(AttachmentDescriptor descriptor) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt);

    private static UserId TripTypeActor(ICurrentUser currentUser) => currentUser.UserId ?? throw TravelTripTypeProblem.NotFound();

    private static UserId ExpenseCategoryActor(ICurrentUser currentUser) => currentUser.UserId ?? throw TravelExpenseCategoryProblem.NotFound();

    private static CatalogMoveDirection TripTypeDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw TravelTripTypeProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection ExpenseCategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw TravelExpenseCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> CreateTripTypeAsync(TravelTripTypeRequest request, TravelTripTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, TripTypeActor(user), token);
        return TypedResults.Created($"/api/travel/trip-types/{value.Id}", value);
    }

    private static async Task<IResult> UpdateTripTypeAsync(int tripTypeId, TravelTripTypeRequest request, TravelTripTypeManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(tripTypeId, request, TripTypeActor(user), token));

    private static async Task<IResult> MoveTripTypeAsync(int tripTypeId, CatalogMoveRequest request, TravelTripTypeManagementService service, CancellationToken token)
    {
        await service.MoveAsync(tripTypeId, TripTypeDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> TripTypeImpactAsync(int tripTypeId, TravelTripTypeManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(tripTypeId, token));

    private static async Task<IResult> DeleteTripTypeAsync(int tripTypeId, TravelTripTypeManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(tripTypeId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteTripTypeAsync(int tripTypeId, CatalogReplacementRequest request, TravelTripTypeManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(tripTypeId, request, TripTypeActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateExpenseCategoryAsync(TravelExpenseCategoryRequest request, TravelExpenseCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, ExpenseCategoryActor(user), token);
        return TypedResults.Created($"/api/travel/expense-categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateExpenseCategoryAsync(int expenseCategoryId, TravelExpenseCategoryRequest request, TravelExpenseCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(expenseCategoryId, request, ExpenseCategoryActor(user), token));

    private static async Task<IResult> MoveExpenseCategoryAsync(int expenseCategoryId, CatalogMoveRequest request, TravelExpenseCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(expenseCategoryId, ExpenseCategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ExpenseCategoryImpactAsync(int expenseCategoryId, TravelExpenseCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(expenseCategoryId, token));

    private static async Task<IResult> DeleteExpenseCategoryAsync(int expenseCategoryId, TravelExpenseCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(expenseCategoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteExpenseCategoryAsync(int expenseCategoryId, CatalogReplacementRequest request, TravelExpenseCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(expenseCategoryId, request, ExpenseCategoryActor(user), token);
        return TypedResults.NoContent();
    }
}
