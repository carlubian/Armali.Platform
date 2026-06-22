using System.Globalization;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Destinations.Queries;

/// <summary>Resolves the destination thumbnail: primary image, first image, or placeholder.</summary>
internal static class DestinationThumbnailResolver
{
    public const string PrimarySource = "primary";
    public const string ImageSource = "image";
    public const string PlaceholderSource = "placeholder";

    public static DestinationThumbnailResponse Resolve(
        int destinationId,
        int? primaryAttachmentId,
        IReadOnlyList<AttachmentDescriptor> descriptors)
    {
        var images = descriptors
            .Where(descriptor => DestinationsAttachments.IsImageContentType(descriptor.ContentType))
            .ToArray();

        if (primaryAttachmentId is { } primaryId
            && images.FirstOrDefault(descriptor => descriptor.Id.Value == primaryId) is { } primary)
        {
            return new(
                primary.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(destinationId, primary.Id.Value),
                PrimarySource);
        }

        if (images.Length > 0)
        {
            var first = images[0];
            return new(
                first.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(destinationId, first.Id.Value),
                ImageSource);
        }

        return Placeholder();
    }

    public static DestinationThumbnailResponse Placeholder() => new(null, null, PlaceholderSource);

    private static string DownloadUrl(int destinationId, int attachmentId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"/api/destinations/{destinationId}/attachments/{attachmentId}");
}
