using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Processes;

/// <summary>
/// Attachment owner identifiers published by the Processes module. Attachments live at
/// the process level only; steps have none.
/// </summary>
internal static class ProcessesAttachments
{
    public const string Module = "Processes";
    public const string ProcessEntityType = "Process";

    public static AttachmentOwner ProcessOwner(int processId) =>
        new(Module, ProcessEntityType, processId.ToString(CultureInfo.InvariantCulture));
}
