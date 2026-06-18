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
}
