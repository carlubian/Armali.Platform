using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Clothes;

/// <summary>Attachment owner identifiers published by the Clothes module.</summary>
internal static class ClothesAttachments
{
    public const string Module = "Clothes";
    public const string GarmentEntityType = "Garment";

    public static AttachmentOwner GarmentOwner(int garmentId) =>
        new(Module, GarmentEntityType, garmentId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Only image attachments can drive the gallery thumbnail or be marked as the
    /// primary image. The attachment subsystem stores canonical content types
    /// (<c>image/jpeg</c>, <c>image/png</c>, <c>image/webp</c>), so a prefix match is
    /// sufficient and stable.
    /// </summary>
    public static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
