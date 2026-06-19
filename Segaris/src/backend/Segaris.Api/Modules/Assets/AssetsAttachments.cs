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
}
