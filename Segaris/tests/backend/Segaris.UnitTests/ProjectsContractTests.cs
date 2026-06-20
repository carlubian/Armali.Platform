using System.Text.Json;
using Segaris.Api.Modules.Projects;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class ProjectsContractTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Fixed_status_vocabulary_is_frozen()
    {
        Assert.Equal(
            ["Planning", "Active", "Completed", "OnHold", "Cancelled"],
            Enum.GetNames<ProjectStatus>());
    }

    [Fact]
    public void Fixed_visibility_vocabulary_is_the_platform_baseline()
    {
        Assert.Equal(["Public", "Private"], Enum.GetNames<RecordVisibility>());
    }

    [Fact]
    public void Creation_defaults_and_limits_are_frozen()
    {
        Assert.Equal(ProjectStatus.Planning, ProjectsDefaults.Status);
        Assert.Equal(RecordVisibility.Public, ProjectsDefaults.Visibility);
        Assert.Equal(200, ProjectsDefaults.NameMaximumLength);
        Assert.Equal(4, ProjectsDefaults.CodeLength);
        Assert.Equal(1000, ProjectsDefaults.RiskDescriptionMaximumLength);
        Assert.Equal(1, ProjectsDefaults.RiskFactorMinimum);
        Assert.Equal(5, ProjectsDefaults.RiskFactorMaximum);
    }

    [Fact]
    public void Risk_band_thresholds_are_frozen()
    {
        Assert.Equal(100, ProjectRiskScoring.HighThreshold);
        Assert.Equal(60, ProjectRiskScoring.MediumThreshold);
    }

    [Fact]
    public void Risk_score_is_the_product_of_the_three_factors()
    {
        Assert.Equal(1, ProjectRiskScoring.Score(1, 1, 1));
        Assert.Equal(125, ProjectRiskScoring.Score(5, 5, 5));
        Assert.Equal(60, ProjectRiskScoring.Score(3, 4, 5));
    }

    [Theory]
    [InlineData(1, "Low")]
    [InlineData(59, "Low")]
    [InlineData(60, "Medium")]
    [InlineData(99, "Medium")]
    [InlineData(100, "High")]
    [InlineData(125, "High")]
    public void Risk_band_classification_uses_the_inclusive_boundaries(int score, string expected)
    {
        Assert.Equal(expected, ProjectRiskScoring.BandFor(score).ToString());
    }

    [Fact]
    public void Unified_identifier_uses_the_frozen_format()
    {
        Assert.Equal(6, ProjectIdentifier.NumberDigits);
        Assert.Equal(
            "INFRWEBS-000123 Cellar renovation",
            ProjectIdentifier.Format("INFR", "WEBS", 123, "Cellar renovation"));
        Assert.Equal(
            "AAAABBBB-123456 X",
            ProjectIdentifier.Format("AAAA", "BBBB", 123456, "X"));
    }

    [Fact]
    public void Routes_freeze_tree_project_activity_risk_and_attachment_shapes()
    {
        Assert.Equal("projects/tree/programs", ProjectsApiRoutes.TreePrograms);
        Assert.Equal("projects/tree/programs/{programId:int}/axes", ProjectsApiRoutes.TreeAxesByProgram);
        Assert.Equal("projects/tree/axes/{axisId:int}/items", ProjectsApiRoutes.TreeItemsByAxis);
        Assert.Equal("projects/projects", ProjectsApiRoutes.Projects);
        Assert.Equal("/{projectId:int}", ProjectsApiRoutes.ProjectById);
        Assert.Equal("projects/activities", ProjectsApiRoutes.Activities);
        Assert.Equal("/{activityId:int}", ProjectsApiRoutes.ActivityById);
        Assert.Equal("/{projectId:int}/risks", ProjectsApiRoutes.ProjectRisks);
        Assert.Equal("/{projectId:int}/risks/{riskId:int}", ProjectsApiRoutes.ProjectRiskById);
        Assert.Equal("/{projectId:int}/attachments", ProjectsApiRoutes.ProjectAttachments);
        Assert.Equal("/{projectId:int}/attachments/{attachmentId}", ProjectsApiRoutes.ProjectAttachmentById);
        Assert.Equal("projects/programs", ProjectsApiRoutes.Programs);
        Assert.Equal("projects/axes", ProjectsApiRoutes.Axes);
    }

    [Fact]
    public void Tree_ordering_contract_is_frozen()
    {
        Assert.Equal("code", ProjectsQuery.NodeOrderField);
        Assert.Equal("number", ProjectsQuery.ItemOrderField);
        Assert.Equal("asc", ProjectsQuery.OrderDirection);
    }

    [Fact]
    public void Configuration_facing_managed_nodes_are_explicit()
    {
        Assert.Empty(ProjectsConfigurationContracts.SharedReferenceKinds);
        Assert.Equal(
            [ProjectsStructuralNodeKind.Program, ProjectsStructuralNodeKind.Axis],
            ProjectsConfigurationContracts.ManagedNodes.Select(node => node.Kind).ToArray());

        var program = ProjectsConfigurationContracts.ManagedNodes[0];
        Assert.Equal("programs", program.RouteSegment);
        Assert.False(program.HasParent);

        var axis = ProjectsConfigurationContracts.ManagedNodes[1];
        Assert.Equal("axes", axis.RouteSegment);
        Assert.True(axis.HasParent);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("projects.project.not_found", ProjectsErrorCodes.ProjectNotFound.Value);
        Assert.Equal("projects.project.validation", ProjectsErrorCodes.ProjectValidation.Value);
        Assert.Equal(
            "projects.project.visibility_forbidden",
            ProjectsErrorCodes.ProjectVisibilityForbidden.Value);
        Assert.Equal("projects.activity.not_found", ProjectsErrorCodes.ActivityNotFound.Value);
        Assert.Equal("projects.risk.not_found", ProjectsErrorCodes.RiskNotFound.Value);
        Assert.Equal("projects.risk.validation", ProjectsErrorCodes.RiskValidation.Value);
        Assert.Equal("projects.attachment.not_found", ProjectsErrorCodes.AttachmentNotFound.Value);
        Assert.Equal("projects.attachment.invalid", ProjectsErrorCodes.AttachmentInvalid.Value);
        Assert.Equal("projects.program.not_found", ProjectsErrorCodes.ProgramNotFound.Value);
        Assert.Equal("projects.program.duplicate_code", ProjectsErrorCodes.ProgramDuplicateCode.Value);
        Assert.Equal("projects.axis.duplicate_code", ProjectsErrorCodes.AxisDuplicateCode.Value);
        Assert.Equal(
            "projects.structure.reassignment_required",
            ProjectsErrorCodes.ReassignmentRequired.Value);
        Assert.Equal(
            "projects.structure.no_compatible_target",
            ProjectsErrorCodes.NoCompatibleTarget.Value);
        Assert.Equal(
            "projects.structure.invalid_target",
            ProjectsErrorCodes.InvalidReassignmentTarget.Value);
    }

    [Fact]
    public void Attachment_owner_uses_the_project_kind()
    {
        var owner = ProjectsAttachments.ProjectOwner(42);

        Assert.Equal(
            ("Projects", "Project", "42"),
            (owner.Module, owner.EntityType, owner.EntityId));
    }

    [Fact]
    public void Launcher_attention_key_is_frozen()
    {
        Assert.Equal("projects", ProjectsLauncherCard.ModuleKey);
    }

    [Fact]
    public void Project_request_serializes_to_the_frozen_wire_shape()
    {
        var request = new CreateProjectRequest(
            AxisId: 7,
            Name: "Cellar renovation",
            Status: "Planning",
            Visibility: "Public");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal(7, root.GetProperty("axisId").GetInt32());
        Assert.Equal("Cellar renovation", root.GetProperty("name").GetString());
        Assert.Equal("Planning", root.GetProperty("status").GetString());
        Assert.Equal("Public", root.GetProperty("visibility").GetString());

        // The global number is system-assigned and is never part of the request.
        Assert.False(root.TryGetProperty("number", out _));
    }

    [Fact]
    public void Risk_request_serializes_factors_without_a_client_score()
    {
        var request = new ProjectRiskRequest(
            Description: "Supplier delay",
            Probability: 3,
            Impact: 4,
            Mitigation: 5);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, Web));
        var root = document.RootElement;
        Assert.Equal("Supplier delay", root.GetProperty("description").GetString());
        Assert.Equal(3, root.GetProperty("probability").GetInt32());
        Assert.Equal(4, root.GetProperty("impact").GetInt32());
        Assert.Equal(5, root.GetProperty("mitigation").GetInt32());

        // The score is system-computed and is never accepted from the client.
        Assert.False(root.TryGetProperty("score", out _));
    }
}
