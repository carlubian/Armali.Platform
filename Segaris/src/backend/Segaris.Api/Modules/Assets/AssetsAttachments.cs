using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Assets;

/// <summary>Attachment owner identifiers published by the Assets module.</summary>
internal static class AssetsAttachments
{
    public const string Module = "Assets";
    public const string AssetEntityType = "Asset";

    public static AttachmentOwner AssetOwner(int assetId) =>
        new(Module, AssetEntityType, assetId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Only image attachments can drive the table thumbnail or be marked as primary.
    /// The shared attachment service stores canonical image media types, so the
    /// stable image/* prefix is sufficient here.
    /// </summary>
    public static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
