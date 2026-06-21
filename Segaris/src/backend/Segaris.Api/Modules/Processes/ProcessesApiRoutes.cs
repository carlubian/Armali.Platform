namespace Segaris.Api.Modules.Processes;

/// <summary>
/// Frozen route shapes for the Processes HTTP surface. The process, step, and
/// attachment routes live under the module-owned <c>processes</c> prefix. The category
/// management routes are also module-owned and are only <em>presented</em> through the
/// Configuration experience; Configuration never queries the Processes tables directly.
/// </summary>
internal static class ProcessesApiRoutes
{
    public const string Tag = "Processes";

    // Process lifecycle.
    public const string Processes = "processes";
    public const string ProcessById = "/{processId:int}";

    // Terminal Cancelled override.
    public const string ProcessCancel = "/{processId:int}/cancel";
    public const string ProcessReopen = "/{processId:int}/reopen";

    // Step list (a process sub-resource): the GET reads the ordered list and the PUT
    // performs the full-collection restructure that preserves state by step identity.
    public const string ProcessSteps = "/{processId:int}/steps";

    // Frontier actions on a single step.
    public const string StepComplete = "/{processId:int}/steps/{stepId:int}/complete";
    public const string StepSkip = "/{processId:int}/steps/{stepId:int}/skip";
    public const string StepUndo = "/{processId:int}/steps/{stepId:int}/undo";

    // Process attachments (a process sub-resource); steps have none.
    public const string ProcessAttachments = "/{processId:int}/attachments";
    public const string ProcessAttachmentById = "/{processId:int}/attachments/{attachmentId}";

    // Module-owned category management, presented through Configuration. The public
    // read and the administrative mutations share the same base segment.
    public const string Categories = "processes/categories";
    public const string CategoryById = "/{categoryId:int}";
    public const string CategoryMove = "/{categoryId:int}/move";
    public const string CategoryDeletionImpact = "/{categoryId:int}/deletion-impact";
    public const string CategoryReplaceAndDelete = "/{categoryId:int}/replace-and-delete";
}
