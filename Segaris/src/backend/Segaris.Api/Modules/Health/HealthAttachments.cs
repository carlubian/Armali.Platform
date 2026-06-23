using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Health;

/// <summary>Attachment owner kinds published by Health.</summary>
internal static class HealthAttachments
{
    public const string Module = "Health";
    public const string MedicineOwnerKind = "Medicine";

    public static AttachmentOwner MedicineOwner(int medicineId) =>
        new(Module, MedicineOwnerKind, medicineId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Only image attachments can drive the gallery thumbnail or be marked as the
    /// primary image. The attachment subsystem stores canonical content types
    /// (<c>image/jpeg</c>, <c>image/png</c>, <c>image/webp</c>), so a prefix match is
    /// sufficient and stable.
    /// </summary>
    public static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
