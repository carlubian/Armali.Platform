using System.Text.Json;
using Segaris.Api.Modules.Health;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class HealthContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Creation_defaults_and_validation_bounds_are_frozen()
    {
        Assert.Equal(RecordVisibility.Public, HealthDefaults.Visibility);
        Assert.False(HealthDefaults.RequiresPrescription);
        Assert.Equal(200, HealthDefaults.NameMaximumLength);
        Assert.Equal(2000, HealthDefaults.SymptomsMaximumLength);
        Assert.Equal(2000, HealthDefaults.PosologyMaximumLength);
        Assert.Equal(2000, HealthDefaults.NotesMaximumLength);
        Assert.Equal(200, HealthDefaults.CategoryNameMaximumLength);
        Assert.Equal((1, 100_000), (HealthDefaults.MinimumAverageDurationDays, HealthDefaults.MaximumAverageDurationDays));
    }

    [Fact]
    public void Routes_freeze_diseases_medicines_associations_attachments_and_categories()
    {
        Assert.Equal("health", HealthApiRoutes.Health);
        Assert.Equal("health/diseases", HealthApiRoutes.Diseases);
        Assert.Equal("/diseases/{diseaseId:int}", HealthApiRoutes.DiseaseById);
        Assert.Equal("/diseases/{diseaseId:int}/medicines", HealthApiRoutes.DiseaseMedicines);
        Assert.Equal("/diseases/{diseaseId:int}/medicines/{medicineId:int}", HealthApiRoutes.DiseaseMedicineById);
        Assert.Equal("health/medicines", HealthApiRoutes.Medicines);
        Assert.Equal("/medicines/{medicineId:int}", HealthApiRoutes.MedicineById);
        Assert.Equal("/medicines/{medicineId:int}/diseases", HealthApiRoutes.MedicineDiseases);
        Assert.Equal("/medicines/{medicineId:int}/diseases/{diseaseId:int}", HealthApiRoutes.MedicineDiseaseById);
        Assert.Equal("/medicines/{medicineId:int}/attachments", HealthApiRoutes.MedicineAttachments);
        Assert.Equal("/medicines/{medicineId:int}/attachments/{attachmentId}", HealthApiRoutes.MedicineAttachmentById);
        Assert.Equal("/medicines/{medicineId:int}/attachments/{attachmentId}/primary", HealthApiRoutes.MedicinePrimaryAttachment);
        Assert.Equal("health/disease-categories", HealthApiRoutes.DiseaseCategories);
        Assert.Equal("health/medicine-categories", HealthApiRoutes.MedicineCategories);
    }

    [Fact]
    public void Disease_and_medicine_query_contracts_are_frozen()
    {
        Assert.Equal(new HashSet<string>(StringComparer.Ordinal) { "name", "category", "id" }, DiseaseQuery.AllowedSortFields);
        Assert.Equal(new HashSet<string>(StringComparer.Ordinal) { "name", "category", "id" }, MedicineQuery.AllowedSortFields);
        Assert.Equal("name", DiseaseQuery.SortFields.Default);
        Assert.Equal("name", MedicineQuery.SortFields.Default);
        Assert.Equal("id", DiseaseQuery.SortFields.TieBreaker);
        Assert.Equal("id", MedicineQuery.SortFields.TieBreaker);
        Assert.Equal("asc", DiseaseQuery.DefaultSortDirection);
        Assert.Equal("asc", MedicineQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], DiseaseQuery.PageSizeOptions);
        Assert.Equal([10, 25, 50, 100], MedicineQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_sorts_are_name_ascending_with_identifier_tie_breaker()
    {
        var diseaseSort = SortRequest.Create(null, null, DiseaseQuery.AllowedSortFields, DiseaseQuery.SortFields.Default, DiseaseQuery.SortFields.TieBreaker);
        var medicineSort = SortRequest.Create(null, null, MedicineQuery.AllowedSortFields, MedicineQuery.SortFields.Default, MedicineQuery.SortFields.TieBreaker);

        Assert.Equal(("name", SortDirection.Ascending, "id"), (diseaseSort.Field, diseaseSort.Direction, diseaseSort.TieBreakerField));
        Assert.Equal(("name", SortDirection.Ascending, "id"), (medicineSort.Field, medicineSort.Direction, medicineSort.TieBreakerField));
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(HealthConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [HealthCatalogKind.DiseaseCategories, HealthCatalogKind.MedicineCategories],
            HealthConfigurationContracts.OwnedCatalogs.Select(descriptor => descriptor.Kind).ToArray());

        Assert.Equal(("disease-categories", true, false), (
            HealthConfigurationContracts.OwnedCatalogs[0].RouteSegment,
            HealthConfigurationContracts.OwnedCatalogs[0].IsRequired,
            HealthConfigurationContracts.OwnedCatalogs[0].SupportsClearing));
        Assert.Equal(("medicine-categories", true, false), (
            HealthConfigurationContracts.OwnedCatalogs[1].RouteSegment,
            HealthConfigurationContracts.OwnedCatalogs[1].IsRequired,
            HealthConfigurationContracts.OwnedCatalogs[1].SupportsClearing));
    }

    [Fact]
    public void Inventory_contract_consumption_is_explicit()
    {
        Assert.Equal(typeof(IInventoryItemReferenceReader), HealthInventoryContracts.ItemReferenceReader);
        Assert.Equal(typeof(IInventoryItemDeletionReferenceHandler), HealthInventoryContracts.ItemDeletionReferenceHandler);
    }

    [Fact]
    public void Association_visibility_rule_is_frozen()
    {
        Assert.Equal(
            [
                HealthAssociationVisibilityRule.AccessibleOnlyCreation,
                HealthAssociationVisibilityRule.ViewerFilteredReads,
                HealthAssociationVisibilityRule.PublishGuard,
            ],
            HealthAssociationContracts.VisibilityRules);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("health.disease.not_found", HealthErrorCodes.DiseaseNotFound.Value);
        Assert.Equal("health.disease.validation", HealthErrorCodes.DiseaseValidation.Value);
        Assert.Equal("health.medicine.not_found", HealthErrorCodes.MedicineNotFound.Value);
        Assert.Equal("health.medicine.validation", HealthErrorCodes.MedicineValidation.Value);
        Assert.Equal("health.medicine.item_not_accessible", HealthErrorCodes.MedicineItemNotAccessible.Value);
        Assert.Equal("health.medicine.item_visibility_forbidden", HealthErrorCodes.MedicineItemVisibilityForbidden.Value);
        Assert.Equal("health.association.not_accessible", HealthErrorCodes.AssociationNotAccessible.Value);
        Assert.Equal("health.association.visibility_forbidden", HealthErrorCodes.AssociationVisibilityForbidden.Value);
        Assert.Equal("health.association.publish_blocked", HealthErrorCodes.AssociationPublishBlocked.Value);
        Assert.Equal("health.attachment.not_found", HealthErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("health.disease_category.not_found", HealthErrorCodes.DiseaseCategoryNotFound.Value);
        Assert.Equal("health.medicine_category.not_found", HealthErrorCodes.MedicineCategoryNotFound.Value);
    }

    [Fact]
    public void Attachment_owner_uses_medicine_kind()
    {
        var owner = HealthAttachments.MedicineOwner(12);

        Assert.Equal(("Health", "Medicine", "12"), (owner.Module, owner.EntityType, owner.EntityId));
    }

    [Fact]
    public void Disease_and_medicine_requests_serialize_to_the_frozen_wire_shape()
    {
        var disease = new CreateDiseaseRequest("Flu", 1, "Fever", 7, null, "Public");
        var medicine = new CreateMedicineRequest("Ibuprofen", 2, "With food", null, 8, null, "Private");

        using var diseaseDocument = JsonDocument.Parse(JsonSerializer.Serialize(disease, Web));
        using var medicineDocument = JsonDocument.Parse(JsonSerializer.Serialize(medicine, Web));

        Assert.Equal(7, diseaseDocument.RootElement.GetProperty("averageDurationDays").GetInt32());
        Assert.Equal(JsonValueKind.Null, medicineDocument.RootElement.GetProperty("requiresPrescription").ValueKind);
        Assert.Equal(8, medicineDocument.RootElement.GetProperty("inventoryItemId").GetInt32());
    }
}
