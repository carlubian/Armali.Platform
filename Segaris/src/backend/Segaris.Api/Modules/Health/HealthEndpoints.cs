using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Health.Mutations;
using Segaris.Api.Modules.Health.Queries;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Maps the frozen Health HTTP surface. Wave 0 exposed route metadata only; Wave 1
/// replaces the category placeholders with the persisted disease and medicine
/// category catalogue read and administrator management behavior surfaced through
/// Configuration. Later waves replace the remaining disease, medicine, association,
/// and attachment placeholders.
/// </summary>
internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup(HealthApiRoutes.Health, HealthApiRoutes.Tag)
            .RequireAuthorization();

        MapDiseaseEndpoints(group);
        MapMedicineEndpoints(group);
        MapCategoryEndpoints(group);
        MapMedicineCategoryEndpoints(group);

        return endpoints;
    }

    private static void MapDiseaseEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/diseases", ListDiseasesAsync)
            .WithName("ListHealthDiseases")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible Health diseases")
            .Produces<PaginatedResponse<DiseaseSummaryResponse>>();
        group.MapPost("/diseases", CreateDiseaseAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateHealthDisease")
            .WithSummary("Creates a Health disease")
            .Produces<DiseaseResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet(HealthApiRoutes.DiseaseById, GetDiseaseAsync)
            .WithName("GetHealthDisease")
            .WithSummary("Returns the detail of an accessible Health disease")
            .Produces<DiseaseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(HealthApiRoutes.DiseaseById, UpdateDiseaseAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateHealthDisease")
            .WithSummary("Replaces an accessible Health disease")
            .Produces<DiseaseResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete(HealthApiRoutes.DiseaseById, DeleteDiseaseAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthDisease")
            .WithSummary("Deletes an accessible Health disease")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet(HealthApiRoutes.DiseaseMedicines, Placeholder)
            .WithName("ListHealthDiseaseMedicines")
            .Produces<IReadOnlyList<MedicineSummaryResponse>>();
        group.MapPost(HealthApiRoutes.DiseaseMedicineById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("AddHealthDiseaseMedicine")
            .Produces(StatusCodes.Status204NoContent);
        group.MapDelete(HealthApiRoutes.DiseaseMedicineById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("RemoveHealthDiseaseMedicine")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapMedicineEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/medicines", ListMedicinesAsync)
            .WithName("ListHealthMedicines")
            .WithSummary("Returns a paginated, filtered, and sorted list of accessible Health medicines")
            .Produces<PaginatedResponse<MedicineSummaryResponse>>();
        group.MapPost("/medicines", CreateMedicineAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateHealthMedicine")
            .WithSummary("Creates a Health medicine")
            .Produces<MedicineResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet(HealthApiRoutes.MedicineById, GetMedicineAsync)
            .WithName("GetHealthMedicine")
            .WithSummary("Returns the detail of an accessible Health medicine")
            .Produces<MedicineResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(HealthApiRoutes.MedicineById, UpdateMedicineAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateHealthMedicine")
            .WithSummary("Replaces an accessible Health medicine")
            .Produces<MedicineResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete(HealthApiRoutes.MedicineById, DeleteMedicineAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthMedicine")
            .WithSummary("Deletes an accessible Health medicine")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapGet(HealthApiRoutes.MedicineDiseases, Placeholder)
            .WithName("ListHealthMedicineDiseases")
            .Produces<IReadOnlyList<DiseaseSummaryResponse>>();
        group.MapPost(HealthApiRoutes.MedicineDiseaseById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("AddHealthMedicineDisease")
            .Produces(StatusCodes.Status204NoContent);
        group.MapDelete(HealthApiRoutes.MedicineDiseaseById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("RemoveHealthMedicineDisease")
            .Produces(StatusCodes.Status204NoContent);
        group.MapGet(HealthApiRoutes.MedicineAttachments, Placeholder)
            .WithName("ListHealthMedicineAttachments")
            .Produces<IReadOnlyList<MedicineAttachmentResponse>>();
        group.MapPost(HealthApiRoutes.MedicineAttachments, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UploadHealthMedicineAttachment")
            .Produces<MedicineAttachmentResponse>(StatusCodes.Status201Created);
        group.MapGet(HealthApiRoutes.MedicineAttachmentById, Placeholder)
            .WithName("DownloadHealthMedicineAttachment");
        group.MapDelete(HealthApiRoutes.MedicineAttachmentById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthMedicineAttachment")
            .Produces(StatusCodes.Status204NoContent);
        group.MapPut(HealthApiRoutes.MedicinePrimaryAttachment, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("SetHealthMedicinePrimaryAttachment")
            .Produces<MedicineAttachmentResponse>();
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/disease-categories", ListDiseaseCategoriesAsync)
            .WithName("ListHealthDiseaseCategories")
            .WithSummary("Returns the Health disease category catalogue")
            .Produces<IReadOnlyList<DiseaseCategoryResponse>>();

        var categories = group.MapGroup("/disease-categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateDiseaseCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateHealthDiseaseCategory")
            .Produces<DiseaseCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(HealthApiRoutes.DiseaseCategoryById, UpdateDiseaseCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateHealthDiseaseCategory")
            .Produces<DiseaseCategoryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(HealthApiRoutes.DiseaseCategoryMove, MoveDiseaseCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("MoveHealthDiseaseCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(HealthApiRoutes.DiseaseCategoryDeletionImpact, DiseaseCategoryImpactAsync)
            .WithName("GetHealthDiseaseCategoryDeletionImpact")
            .Produces<CatalogDeletionImpactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(HealthApiRoutes.DiseaseCategoryById, DeleteDiseaseCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthDiseaseCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(HealthApiRoutes.DiseaseCategoryReplaceAndDelete, ReplaceAndDeleteDiseaseCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReplaceAndDeleteHealthDiseaseCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapMedicineCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/medicine-categories", ListMedicineCategoriesAsync)
            .WithName("ListHealthMedicineCategories")
            .WithSummary("Returns the Health medicine category catalogue")
            .Produces<IReadOnlyList<MedicineCategoryResponse>>();

        var categories = group.MapGroup("/medicine-categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", CreateMedicineCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateHealthMedicineCategory")
            .Produces<MedicineCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut(HealthApiRoutes.MedicineCategoryById, UpdateMedicineCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateHealthMedicineCategory")
            .Produces<MedicineCategoryResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(HealthApiRoutes.MedicineCategoryMove, MoveMedicineCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("MoveHealthMedicineCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapGet(HealthApiRoutes.MedicineCategoryDeletionImpact, MedicineCategoryImpactAsync)
            .WithName("GetHealthMedicineCategoryDeletionImpact")
            .Produces<CatalogDeletionImpactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapDelete(HealthApiRoutes.MedicineCategoryById, DeleteMedicineCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthMedicineCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPost(HealthApiRoutes.MedicineCategoryReplaceAndDelete, ReplaceAndDeleteMedicineCategoryAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ReplaceAndDeleteHealthMedicineCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static UserId DiseaseCategoryActor(ICurrentUser currentUser) =>
        currentUser.UserId ?? throw DiseaseCategoryProblem.NotFound();

    private static UserId MedicineCategoryActor(ICurrentUser currentUser) =>
        currentUser.UserId ?? throw MedicineCategoryProblem.NotFound();

    private static CatalogMoveDirection DiseaseCategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw DiseaseCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static CatalogMoveDirection MedicineCategoryDirection(CatalogMoveRequest request) =>
        CatalogMoveDirections.TryParse(request.Direction, out var direction)
            ? direction
            : throw MedicineCategoryProblem.Validation("direction", "Direction must be 'up' or 'down'.");

    private static async Task<IResult> ListDiseaseCategoriesAsync(HealthCatalogReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListDiseaseCategoriesAsync(token));

    private static async Task<IResult> CreateDiseaseCategoryAsync(
        CatalogItemRequest request, DiseaseCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, DiseaseCategoryActor(user), token);
        return TypedResults.Created($"/api/health/disease-categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateDiseaseCategoryAsync(
        int categoryId, CatalogItemRequest request, DiseaseCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, DiseaseCategoryActor(user), token));

    private static async Task<IResult> MoveDiseaseCategoryAsync(
        int categoryId, CatalogMoveRequest request, DiseaseCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, DiseaseCategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DiseaseCategoryImpactAsync(
        int categoryId, DiseaseCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteDiseaseCategoryAsync(
        int categoryId, DiseaseCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteDiseaseCategoryAsync(
        int categoryId, CatalogReplacementRequest request, DiseaseCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, DiseaseCategoryActor(user), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListMedicineCategoriesAsync(HealthCatalogReadService read, CancellationToken token) =>
        TypedResults.Ok(await read.ListMedicineCategoriesAsync(token));

    private static async Task<IResult> ListDiseasesAsync(
        [AsParameters] DiseaseListQuery query,
        DiseaseReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListDiseasesAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetDiseaseAsync(
        int diseaseId,
        DiseaseReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var disease = await read.GetDiseaseAsync(diseaseId, userId, cancellationToken);
        if (disease is null)
        {
            throw HealthDiseaseProblem.NotFound();
        }

        return TypedResults.Ok(disease);
    }

    private static async Task<IResult> CreateDiseaseAsync(
        CreateDiseaseRequest request,
        DiseaseWriteService write,
        DiseaseReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int diseaseId;
        try
        {
            diseaseId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (HealthValidationException exception)
        {
            throw HealthDiseaseProblem.From(exception);
        }

        var created = await read.GetDiseaseAsync(diseaseId, userId, cancellationToken);
        return TypedResults.Created($"/api/health/diseases/{diseaseId}", created);
    }

    private static async Task<IResult> UpdateDiseaseAsync(
        int diseaseId,
        UpdateDiseaseRequest request,
        DiseaseWriteService write,
        DiseaseReadService read,
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
            updated = await write.UpdateAsync(diseaseId, request, userId, cancellationToken);
        }
        catch (HealthValidationException exception)
        {
            throw HealthDiseaseProblem.From(exception);
        }

        if (!updated)
        {
            throw HealthDiseaseProblem.NotFound();
        }

        var disease = await read.GetDiseaseAsync(diseaseId, userId, cancellationToken);
        return TypedResults.Ok(disease);
    }

    private static async Task<IResult> DeleteDiseaseAsync(
        int diseaseId,
        DiseaseWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(diseaseId, userId, cancellationToken);
        if (!deleted)
        {
            throw HealthDiseaseProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListMedicinesAsync(
        [AsParameters] MedicineListQuery query,
        MedicineReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var result = await read.ListMedicinesAsync(
            query.ToFilter(),
            query.ToPagination(),
            query.ToSort(),
            userId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetMedicineAsync(
        int medicineId,
        MedicineReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var medicine = await read.GetMedicineAsync(medicineId, userId, cancellationToken);
        if (medicine is null)
        {
            throw HealthMedicineProblem.NotFound();
        }

        return TypedResults.Ok(medicine);
    }

    private static async Task<IResult> CreateMedicineAsync(
        CreateMedicineRequest request,
        MedicineWriteService write,
        MedicineReadService read,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        int medicineId;
        try
        {
            medicineId = await write.CreateAsync(request, userId, cancellationToken);
        }
        catch (HealthValidationException exception)
        {
            throw HealthMedicineProblem.From(exception);
        }

        var created = await read.GetMedicineAsync(medicineId, userId, cancellationToken);
        return TypedResults.Created($"/api/health/medicines/{medicineId}", created);
    }

    private static async Task<IResult> UpdateMedicineAsync(
        int medicineId,
        UpdateMedicineRequest request,
        MedicineWriteService write,
        MedicineReadService read,
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
            updated = await write.UpdateAsync(medicineId, request, userId, cancellationToken);
        }
        catch (HealthValidationException exception)
        {
            throw HealthMedicineProblem.From(exception);
        }

        if (!updated)
        {
            throw HealthMedicineProblem.NotFound();
        }

        var medicine = await read.GetMedicineAsync(medicineId, userId, cancellationToken);
        return TypedResults.Ok(medicine);
    }

    private static async Task<IResult> DeleteMedicineAsync(
        int medicineId,
        MedicineWriteService write,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return TypedResults.Unauthorized();
        }

        var deleted = await write.DeleteAsync(medicineId, userId, cancellationToken);
        if (!deleted)
        {
            throw HealthMedicineProblem.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> CreateMedicineCategoryAsync(
        CatalogItemRequest request, MedicineCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        var value = await service.CreateAsync(request, MedicineCategoryActor(user), token);
        return TypedResults.Created($"/api/health/medicine-categories/{value.Id}", value);
    }

    private static async Task<IResult> UpdateMedicineCategoryAsync(
        int categoryId, CatalogItemRequest request, MedicineCategoryManagementService service, ICurrentUser user, CancellationToken token) =>
        TypedResults.Ok(await service.UpdateAsync(categoryId, request, MedicineCategoryActor(user), token));

    private static async Task<IResult> MoveMedicineCategoryAsync(
        int categoryId, CatalogMoveRequest request, MedicineCategoryManagementService service, CancellationToken token)
    {
        await service.MoveAsync(categoryId, MedicineCategoryDirection(request), token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> MedicineCategoryImpactAsync(
        int categoryId, MedicineCategoryManagementService service, CancellationToken token) =>
        TypedResults.Ok(await service.ImpactAsync(categoryId, token));

    private static async Task<IResult> DeleteMedicineCategoryAsync(
        int categoryId, MedicineCategoryManagementService service, CancellationToken token)
    {
        await service.DeleteAsync(categoryId, token);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReplaceAndDeleteMedicineCategoryAsync(
        int categoryId, CatalogReplacementRequest request, MedicineCategoryManagementService service, ICurrentUser user, CancellationToken token)
    {
        await service.ReplaceAndDeleteAsync(categoryId, request, MedicineCategoryActor(user), token);
        return TypedResults.NoContent();
    }

    private static IResult Placeholder() => TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
}
