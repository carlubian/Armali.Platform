using System.Globalization;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Assets.Queries;

/// <summary>Resolves the thumbnail: primary image, first image, or placeholder.</summary>
internal static class AssetThumbnailResolver
{
    public const string PrimarySource = "primary";
    public const string ImageSource = "image";
    public const string PlaceholderSource = "placeholder";

    public static AssetThumbnailResponse Resolve(
        int assetId,
        int? primaryAttachmentId,
        IReadOnlyList<AttachmentDescriptor> descriptors)
    {
        var images = descriptors
            .Where(descriptor => AssetsAttachments.IsImageContentType(descriptor.ContentType))
            .ToArray();

        if (primaryAttachmentId is { } primaryId
            && images.FirstOrDefault(descriptor => descriptor.Id.Value == primaryId) is { } primary)
        {
            return new(
                primary.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(assetId, primary.Id.Value),
                PrimarySource);
        }

        if (images.Length > 0)
        {
            var first = images[0];
            return new(
                first.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(assetId, first.Id.Value),
                ImageSource);
        }

        return Placeholder();
    }

    public static AssetThumbnailResponse Placeholder() => new(null, null, PlaceholderSource);

    private static string DownloadUrl(int assetId, int attachmentId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"/api/assets/items/{assetId}/attachments/{attachmentId}");
}
