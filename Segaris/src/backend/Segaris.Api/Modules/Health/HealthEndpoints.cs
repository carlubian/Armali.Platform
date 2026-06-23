using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Health.Contracts;
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
        group.MapGet("/diseases", Placeholder)
            .WithName("ListHealthDiseases")
            .Produces<PaginatedResponse<DiseaseSummaryResponse>>();
        group.MapPost("/diseases", Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateHealthDisease")
            .Produces<DiseaseResponse>(StatusCodes.Status201Created);
        group.MapGet(HealthApiRoutes.DiseaseById, Placeholder)
            .WithName("GetHealthDisease")
            .Produces<DiseaseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(HealthApiRoutes.DiseaseById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateHealthDisease")
            .Produces<DiseaseResponse>();
        group.MapDelete(HealthApiRoutes.DiseaseById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthDisease")
            .Produces(StatusCodes.Status204NoContent);
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
        group.MapGet("/medicines", Placeholder)
            .WithName("ListHealthMedicines")
            .Produces<PaginatedResponse<MedicineSummaryResponse>>();
        group.MapPost("/medicines", Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateHealthMedicine")
            .Produces<MedicineResponse>(StatusCodes.Status201Created);
        group.MapGet(HealthApiRoutes.MedicineById, Placeholder)
            .WithName("GetHealthMedicine")
            .Produces<MedicineResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut(HealthApiRoutes.MedicineById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateHealthMedicine")
            .Produces<MedicineResponse>();
        group.MapDelete(HealthApiRoutes.MedicineById, Placeholder)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteHealthMedicine")
            .Produces(StatusCodes.Status204NoContent);
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
