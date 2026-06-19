using System.Text.Json;
using Segaris.Api.Modules.Maintenance;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class MaintenanceContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Fixed_status_vocabulary_is_frozen()
    {
        Assert.Equal(
            ["Pending", "InProgress", "Completed", "Cancelled"],
            Enum.GetNames<MaintenanceStatus>());
    }

    [Fact]
    public void Fixed_priority_vocabulary_is_frozen()
    {
        Assert.Equal(["Low", "Medium", "High"], Enum.GetNames<MaintenancePriority>());
    }

    [Fact]
    public void Creation_defaults_are_frozen()
    {
        Assert.Equal(MaintenanceStatus.Pending, MaintenanceDefaults.Status);
        Assert.Equal(MaintenancePriority.Medium, MaintenanceDefaults.Priority);
        Assert.Equal(RecordVisibility.Public, MaintenanceDefaults.Visibility);
        Assert.Equal(200, MaintenanceDefaults.TitleMaximumLength);
        Assert.Equal(4000, MaintenanceDefaults.NotesMaximumLength);
    }

    [Fact]
    public void Initial_type_catalogue_is_frozen_and_ordered()
    {
        Assert.Equal(
            ["Repair", "Preventive", "Inspection", "Cleaning", "Installation", "Other"],
            MaintenanceDefaults.InitialTypes);
    }

    [Fact]
    public void Routes_freeze_tasks_types_and_attachments()
    {
        Assert.Equal("maintenance/tasks", MaintenanceApiRoutes.Tasks);
        Assert.Equal("/{taskId:int}", MaintenanceApiRoutes.TaskById);
        Assert.Equal("/{taskId:int}/attachments", MaintenanceApiRoutes.TaskAttachments);
        Assert.Equal(
            "/{taskId:int}/attachments/{attachmentId}",
            MaintenanceApiRoutes.TaskAttachmentById);
        Assert.Equal("maintenance/types", MaintenanceApiRoutes.Types);
    }

    [Fact]
    public void Task_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "title", "type", "status", "priority", "dueDate", "visibility", "id",
            },
            MaintenanceQuery.AllowedSortFields);
        Assert.Equal("dueDate", MaintenanceQuery.SortFields.Default);
        Assert.Equal("id", MaintenanceQuery.SortFields.TieBreaker);
        Assert.Equal("asc", MaintenanceQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], MaintenanceQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_task_sort_is_due_date_ascending_then_identifier()
    {
        var sort = SortRequest.Create(
            null,
            null,
            MaintenanceQuery.AllowedSortFields,
            MaintenanceQuery.SortFields.Default,
            MaintenanceQuery.SortFields.TieBreaker);

        Assert.Equal("dueDate", sort.Field);
        Assert.Equal(SortDirection.Ascending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(MaintenanceConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [MaintenanceCatalogKind.MaintenanceTypes],
            MaintenanceConfigurationContracts.OwnedCatalogs
                .Select(descriptor => descriptor.Kind)
                .ToArray());

        var types = MaintenanceConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("types", types.RouteSegment);
        Assert.True(types.IsRequired);
        Assert.False(types.SupportsClearing);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("maintenance.task.not_found", MaintenanceErrorCodes.TaskNotFound.Value);
        Assert.Equal("maintenance.task.validation", MaintenanceErrorCodes.TaskValidation.Value);
        Assert.Equal(
            "maintenance.task.visibility_forbidden",
            MaintenanceErrorCodes.TaskVisibilityForbidden.Value);
        Assert.Equal("maintenance.task.unknown_type", MaintenanceErrorCodes.UnknownType.Value);
        Assert.Equal("maintenance.task.asset_invalid", MaintenanceErrorCodes.AssetReferenceInvalid.Value);
        Assert.Equal(
            "maintenance.task.asset_visibility_forbidden",
            MaintenanceErrorCodes.AssetVisibilityForbidden.Value);
        Assert.Equal("maintenance.attachment.not_found", MaintenanceErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("maintenance.attachment.invalid", MaintenanceErrorCodes.AttachmentInvalid.Value);
        Assert.Equal("maintenance.type.not_found", MaintenanceErrorCodes.TypeNotFound.Value);
        Assert.Equal("maintenance.type.referenced", MaintenanceErrorCodes.TypeReferenced.Value);
        Assert.Equal(
            "maintenance.asset_deletion.blocked",
            MaintenanceErrorCodes.AssetDeletionBlocked.Value);
    }

    [Fact]
    public void Attachment_owner_uses_maintenance_task_kind()
    {
        var owner = MaintenanceAttachments.TaskOwner(12);

        Assert.Equal(
            ("Maintenance", "MaintenanceTask", "12"),
            (owner.Module, owner.EntityType, owner.EntityId));
    }

    [Fact]
    public void Launcher_attention_key_is_frozen()
    {
        Assert.Equal("maintenance", MaintenanceLauncherCard.ModuleKey);
    }

    [Fact]
    public void Task_request_serializes_type_dates_and_references_to_the_frozen_wire_shape()
    {
        var request = new CreateMaintenanceTaskRequest(
            "Replace filter",
            MaintenanceTypeId: 4,
            Status: "Pending",
            Priority: "High",
            DueDate: new DateOnly(2026, 7, 1),
            Notes: "Before the summer",
            AssetId: 9,
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Replace filter", root.GetProperty("title").GetString());
        Assert.Equal(4, root.GetProperty("maintenanceTypeId").GetInt32());
        Assert.Equal("Pending", root.GetProperty("status").GetString());
        Assert.Equal("High", root.GetProperty("priority").GetString());
        Assert.Equal("2026-07-01", root.GetProperty("dueDate").GetString());
        Assert.Equal("Before the summer", root.GetProperty("notes").GetString());
        Assert.Equal(9, root.GetProperty("assetId").GetInt32());
        Assert.Equal("Public", root.GetProperty("visibility").GetString());

        // The completion date is system-managed and is never part of the request.
        Assert.False(root.TryGetProperty("completedDate", out _));
    }
}
