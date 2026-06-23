using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Health;

/// <summary>Attachment owner kinds published by Health.</summary>
internal static class HealthAttachments
{
    public const string MedicineOwnerKind = "Medicine";

    public static AttachmentOwner MedicineOwner(int medicineId) =>
        new("Health", MedicineOwnerKind, medicineId.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
