using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Firebird;

/// <summary>Attachment owner identifiers published by the Firebird module.</summary>
internal static class FirebirdAttachments
{
    public const string Module = "Firebird";
    public const string PersonEntityType = "Person";

    public static AttachmentOwner PersonOwner(int personId) =>
        new(Module, PersonEntityType, personId.ToString(CultureInfo.InvariantCulture));

    public static bool IsAvatarContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
