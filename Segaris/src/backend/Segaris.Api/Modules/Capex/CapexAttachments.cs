using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Capex;

/// <summary>
/// Frozen attachment ownership for Capex entries. Every attachment operation is
/// scoped to its owning entry through this owner before the platform attachment
/// service is invoked (Wave 4).
/// </summary>
internal static class CapexAttachments
{
    public const string Module = "Capex";

    public const string EntityType = "Entry";

    public static AttachmentOwner Owner(int entryId) =>
        new(Module, EntityType, entryId.ToString(CultureInfo.InvariantCulture));
}
