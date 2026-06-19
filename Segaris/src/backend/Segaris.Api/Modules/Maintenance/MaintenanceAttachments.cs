using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>Attachment owner identifiers published by the Maintenance module.</summary>
internal static class MaintenanceAttachments
{
    public const string Module = "Maintenance";
    public const string TaskEntityType = "MaintenanceTask";

    public static AttachmentOwner TaskOwner(int taskId) =>
        new(Module, TaskEntityType, taskId.ToString(CultureInfo.InvariantCulture));
}
