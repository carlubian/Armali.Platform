using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Maps the frozen Health HTTP surface. Wave 0 exposes route metadata only; later
/// waves replace the placeholder handlers with the persisted disease, medicine,
/// association, attachment, and catalogue behavior.
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
        group.MapGet("/disease-categories", Placeholder)
            .WithName("ListHealthDiseaseCategories")
            .Produces<IReadOnlyList<DiseaseCategoryResponse>>();

        var categories = group.MapGroup("/disease-categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateHealthDiseaseCategory");
        categories.MapPut(HealthApiRoutes.DiseaseCategoryById, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateHealthDiseaseCategory");
        categories.MapPost(HealthApiRoutes.DiseaseCategoryMove, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveHealthDiseaseCategory");
        categories.MapGet(HealthApiRoutes.DiseaseCategoryDeletionImpact, Placeholder).WithName("GetHealthDiseaseCategoryDeletionImpact");
        categories.MapDelete(HealthApiRoutes.DiseaseCategoryById, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteHealthDiseaseCategory");
        categories.MapPost(HealthApiRoutes.DiseaseCategoryReplaceAndDelete, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteHealthDiseaseCategory");
    }

    private static void MapMedicineCategoryEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/medicine-categories", Placeholder)
            .WithName("ListHealthMedicineCategories")
            .Produces<IReadOnlyList<MedicineCategoryResponse>>();

        var categories = group.MapGroup("/medicine-categories").RequireAuthorization(IdentityPolicies.Admin);
        categories.MapPost("", Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("CreateHealthMedicineCategory");
        categories.MapPut(HealthApiRoutes.MedicineCategoryById, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("UpdateHealthMedicineCategory");
        categories.MapPost(HealthApiRoutes.MedicineCategoryMove, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("MoveHealthMedicineCategory");
        categories.MapGet(HealthApiRoutes.MedicineCategoryDeletionImpact, Placeholder).WithName("GetHealthMedicineCategoryDeletionImpact");
        categories.MapDelete(HealthApiRoutes.MedicineCategoryById, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("DeleteHealthMedicineCategory");
        categories.MapPost(HealthApiRoutes.MedicineCategoryReplaceAndDelete, Placeholder).AddEndpointFilter<AntiforgeryEndpointFilter>().WithName("ReplaceAndDeleteHealthMedicineCategory");
    }

    private static IResult Placeholder() => TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
}
