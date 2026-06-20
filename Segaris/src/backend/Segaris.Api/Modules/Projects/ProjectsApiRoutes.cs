namespace Segaris.Api.Modules.Projects;

/// <summary>
/// Frozen route shapes for the Projects HTTP surface. The tree, project, activity,
/// risk, and attachment routes live under the module-owned <c>projects</c> prefix.
/// The program and axis management routes are also module-owned and are only
/// <em>presented</em> through the Configuration experience; Configuration never
/// queries the Projects tables directly.
/// </summary>
internal static class ProjectsApiRoutes
{
    public const string Tag = "Projects";

    // Lazily expanded hierarchy reads.
    public const string TreePrograms = "projects/tree/programs";
    public const string TreeAxesByProgram = "projects/tree/programs/{programId:int}/axes";
    public const string TreeItemsByAxis = "projects/tree/axes/{axisId:int}/items";

    // Project lifecycle.
    public const string Projects = "projects/projects";
    public const string ProjectById = "/{projectId:int}";

    // Activity lifecycle.
    public const string Activities = "projects/activities";
    public const string ActivityById = "/{activityId:int}";

    // Project risks (a project sub-resource).
    public const string ProjectRisks = "/{projectId:int}/risks";
    public const string ProjectRiskById = "/{projectId:int}/risks/{riskId:int}";

    // Project attachments (a project sub-resource); activities have none.
    public const string ProjectAttachments = "/{projectId:int}/attachments";
    public const string ProjectAttachmentById = "/{projectId:int}/attachments/{attachmentId}";

    // Module-owned program management, presented through Configuration.
    public const string Programs = "projects/programs";
    public const string ProgramById = "/{programId:int}";
    public const string ProgramDeletionImpact = "/{programId:int}/deletion-impact";
    public const string ProgramReassignAndDelete = "/{programId:int}/reassign-and-delete";

    // Module-owned axis management, presented through Configuration.
    public const string Axes = "projects/axes";
    public const string AxisById = "/{axisId:int}";
    public const string AxisDeletionImpact = "/{axisId:int}/deletion-impact";
    public const string AxisReassignAndDelete = "/{axisId:int}/reassign-and-delete";
}
