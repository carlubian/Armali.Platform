using System.Text.Json;
using Segaris.Api.Modules.Processes;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class ProcessesContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Derived_status_vocabulary_is_frozen()
    {
        Assert.Equal(
            ["NotStarted", "InProgress", "Completed"],
            Enum.GetNames<ProcessDerivedStatus>());
    }

    [Fact]
    public void Wire_status_vocabulary_appends_the_cancelled_override()
    {
        Assert.Equal(
            ["NotStarted", "InProgress", "Completed", "Cancelled"],
            ProcessExecution.StatusNames);
        Assert.Equal("Cancelled", ProcessExecution.CancelledStatusName);
    }

    [Fact]
    public void Step_execution_state_vocabulary_is_frozen()
    {
        Assert.Equal(
            ["Pending", "Completed", "Skipped"],
            Enum.GetNames<StepExecutionState>());
    }

    [Fact]
    public void Fixed_visibility_vocabulary_is_the_platform_baseline()
    {
        Assert.Equal(["Public", "Private"], Enum.GetNames<RecordVisibility>());
    }

    [Fact]
    public void Creation_defaults_and_limits_are_frozen()
    {
        Assert.Equal(RecordVisibility.Public, ProcessesDefaults.Visibility);
        Assert.Equal(200, ProcessesDefaults.NameMaximumLength);
        Assert.Equal(4000, ProcessesDefaults.NotesMaximumLength);
        Assert.Equal(500, ProcessesDefaults.StepDescriptionMaximumLength);
        Assert.Equal(1000, ProcessesDefaults.StepNotesMaximumLength);
    }

    [Fact]
    public void Initial_category_catalogue_is_frozen_and_ordered()
    {
        Assert.Equal(
            [
                "Administrative",
                "Legal",
                "Tax",
                "Health",
                "Education",
                "Vehicle",
                "Housing",
                "Other",
            ],
            ProcessesDefaults.InitialCategories);
    }

    [Fact]
    public void Routes_freeze_process_step_attachment_and_category_shapes()
    {
        Assert.Equal("processes", ProcessesApiRoutes.Processes);
        Assert.Equal("/{processId:int}", ProcessesApiRoutes.ProcessById);
        Assert.Equal("/{processId:int}/cancel", ProcessesApiRoutes.ProcessCancel);
        Assert.Equal("/{processId:int}/reopen", ProcessesApiRoutes.ProcessReopen);
        Assert.Equal("/{processId:int}/steps", ProcessesApiRoutes.ProcessSteps);
        Assert.Equal(
            "/{processId:int}/steps/{stepId:int}/complete",
            ProcessesApiRoutes.StepComplete);
        Assert.Equal("/{processId:int}/steps/{stepId:int}/skip", ProcessesApiRoutes.StepSkip);
        Assert.Equal("/{processId:int}/steps/{stepId:int}/undo", ProcessesApiRoutes.StepUndo);
        Assert.Equal("/{processId:int}/attachments", ProcessesApiRoutes.ProcessAttachments);
        Assert.Equal(
            "/{processId:int}/attachments/{attachmentId}",
            ProcessesApiRoutes.ProcessAttachmentById);
        Assert.Equal("processes/categories", ProcessesApiRoutes.Categories);
    }

    [Fact]
    public void Process_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "category", "status", "dueDate", "visibility", "id",
            },
            ProcessesQuery.AllowedSortFields);
        Assert.Equal("dueDate", ProcessesQuery.SortFields.Default);
        Assert.Equal("id", ProcessesQuery.SortFields.TieBreaker);
        Assert.Equal("asc", ProcessesQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], ProcessesQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_process_sort_is_effective_due_date_ascending_then_identifier()
    {
        var sort = SortRequest.Create(
            null,
            null,
            ProcessesQuery.AllowedSortFields,
            ProcessesQuery.SortFields.Default,
            ProcessesQuery.SortFields.TieBreaker);

        Assert.Equal("dueDate", sort.Field);
        Assert.Equal(SortDirection.Ascending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Fact]
    public void Configuration_facing_catalog_contracts_are_explicit()
    {
        Assert.Empty(ProcessesConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [ProcessesCatalogKind.ProcessCategories],
            ProcessesConfigurationContracts.OwnedCatalogs
                .Select(descriptor => descriptor.Kind)
                .ToArray());

        var categories = ProcessesConfigurationContracts.OwnedCatalogs[0];
        Assert.Equal("categories", categories.RouteSegment);
        Assert.True(categories.IsRequired);
        Assert.False(categories.SupportsClearing);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("processes.process.not_found", ProcessesErrorCodes.ProcessNotFound.Value);
        Assert.Equal("processes.process.validation", ProcessesErrorCodes.ProcessValidation.Value);
        Assert.Equal(
            "processes.process.visibility_forbidden",
            ProcessesErrorCodes.ProcessVisibilityForbidden.Value);
        Assert.Equal(
            "processes.process.unknown_category",
            ProcessesErrorCodes.UnknownCategory.Value);
        Assert.Equal("processes.step.not_found", ProcessesErrorCodes.StepNotFound.Value);
        Assert.Equal("processes.step.validation", ProcessesErrorCodes.StepValidation.Value);
        Assert.Equal(
            "processes.step.contiguity_violation",
            ProcessesErrorCodes.StepContiguityViolation.Value);
        Assert.Equal(
            "processes.step.frontier_violation",
            ProcessesErrorCodes.StepFrontierViolation.Value);
        Assert.Equal("processes.step.not_optional", ProcessesErrorCodes.StepNotOptional.Value);
        Assert.Equal(
            "processes.attachment.not_found",
            ProcessesErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("processes.attachment.invalid", ProcessesErrorCodes.AttachmentInvalid.Value);
        Assert.Equal("processes.category.not_found", ProcessesErrorCodes.CategoryNotFound.Value);
        Assert.Equal("processes.category.referenced", ProcessesErrorCodes.CategoryReferenced.Value);
    }

    [Fact]
    public void Attachment_owner_uses_the_process_kind()
    {
        var owner = ProcessesAttachments.ProcessOwner(42);

        Assert.Equal(
            ("Processes", "Process", "42"),
            (owner.Module, owner.EntityType, owner.EntityId));
    }

    [Fact]
    public void Launcher_attention_key_is_frozen()
    {
        Assert.Equal("processes", ProcessesLauncherCard.ModuleKey);
    }

    [Fact]
    public void Process_request_serializes_to_the_frozen_wire_shape_without_a_status()
    {
        var request = new CreateProcessRequest(
            Name: "Renew passport",
            CategoryId: 4,
            DueDate: new DateOnly(2026, 7, 1),
            Notes: "Before the summer",
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Renew passport", root.GetProperty("name").GetString());
        Assert.Equal(4, root.GetProperty("categoryId").GetInt32());
        Assert.Equal("2026-07-01", root.GetProperty("dueDate").GetString());
        Assert.Equal("Before the summer", root.GetProperty("notes").GetString());
        Assert.Equal("Public", root.GetProperty("visibility").GetString());

        // The status is system-derived and the Cancelled override is toggled through
        // dedicated routes; neither is ever part of the request.
        Assert.False(root.TryGetProperty("status", out _));
        Assert.False(root.TryGetProperty("isCancelled", out _));
    }

    [Fact]
    public void Step_list_request_serializes_without_a_client_execution_state()
    {
        var request = new UpdateStepListRequest(
        [
            new StepListItemRequest(
                Id: 7,
                Description: "Gather documents",
                DueDate: new DateOnly(2026, 6, 1),
                Notes: null,
                IsOptional: false),
            new StepListItemRequest(
                Id: null,
                Description: "Optional notarisation",
                DueDate: null,
                Notes: null,
                IsOptional: true),
        ]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var steps = document.RootElement.GetProperty("steps");
        Assert.Equal(2, steps.GetArrayLength());

        var first = steps[0];
        Assert.Equal(7, first.GetProperty("id").GetInt32());
        Assert.Equal("Gather documents", first.GetProperty("description").GetString());
        Assert.False(first.GetProperty("isOptional").GetBoolean());

        var second = steps[1];
        Assert.Equal(JsonValueKind.Null, second.GetProperty("id").ValueKind);
        Assert.True(second.GetProperty("isOptional").GetBoolean());

        // The execution state is preserved by step identity on the server and is never
        // accepted from the client.
        Assert.False(first.TryGetProperty("state", out _));
    }
}
