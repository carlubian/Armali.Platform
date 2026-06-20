using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Projects;

/// <summary>
/// Attachment owner identifiers published by the Projects module. Only projects own
/// result-file attachments; activities, programs, and axes own none.
/// </summary>
internal static class ProjectsAttachments
{
    public const string Module = "Projects";
    public const string ProjectEntityType = "Project";

    public static AttachmentOwner ProjectOwner(int projectId) =>
        new(Module, ProjectEntityType, projectId.ToString(CultureInfo.InvariantCulture));
}
