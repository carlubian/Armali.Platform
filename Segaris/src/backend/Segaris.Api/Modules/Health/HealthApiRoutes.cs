namespace Segaris.Api.Modules.Health;

/// <summary>Frozen route shapes for the Health HTTP surface.</summary>
internal static class HealthApiRoutes
{
    public const string Tag = "Health";
    public const string Health = "health";

    public const string Diseases = "health/diseases";
    public const string DiseaseById = "/diseases/{diseaseId:int}";
    public const string DiseaseMedicines = "/diseases/{diseaseId:int}/medicines";
    public const string DiseaseMedicineById = "/diseases/{diseaseId:int}/medicines/{medicineId:int}";

    public const string Medicines = "health/medicines";
    public const string MedicineById = "/medicines/{medicineId:int}";
    public const string MedicineDiseases = "/medicines/{medicineId:int}/diseases";
    public const string MedicineDiseaseById = "/medicines/{medicineId:int}/diseases/{diseaseId:int}";
    public const string MedicineAttachments = "/medicines/{medicineId:int}/attachments";
    public const string MedicineAttachmentById = "/medicines/{medicineId:int}/attachments/{attachmentId}";
    public const string MedicinePrimaryAttachment = "/medicines/{medicineId:int}/attachments/{attachmentId}/primary";

    public const string DiseaseCategories = "health/disease-categories";
    public const string DiseaseCategoryById = "/{categoryId:int}";
    public const string DiseaseCategoryMove = "/{categoryId:int}/move";
    public const string DiseaseCategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string DiseaseCategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";

    public const string MedicineCategories = "health/medicine-categories";
    public const string MedicineCategoryById = "/{categoryId:int}";
    public const string MedicineCategoryMove = "/{categoryId:int}/move";
    public const string MedicineCategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string MedicineCategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";
}
