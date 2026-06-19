namespace Segaris.Api.Modules.Maintenance;

/// <summary>Frozen route shapes for the Maintenance HTTP surface.</summary>
internal static class MaintenanceApiRoutes
{
    public const string Tag = "Maintenance";

    public const string Tasks = "maintenance/tasks";
    public const string TaskById = "/{taskId:int}";
    public const string TaskAttachments = "/{taskId:int}/attachments";
    public const string TaskAttachmentById = "/{taskId:int}/attachments/{attachmentId}";

    public const string Types = "maintenance/types";
    public const string TypeById = "/{typeId:int}";
    public const string TypeMove = "/{typeId:int}/move";
    public const string TypeDeletionImpact = "/{typeId:int}/deletion-impact";
    public const string TypeReplaceAndDelete = "/{typeId:int}/replace-and-delete";
}
