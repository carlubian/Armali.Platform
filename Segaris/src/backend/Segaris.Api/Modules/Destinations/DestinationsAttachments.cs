using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Destinations;

/// <summary>Attachment owner identifiers published by the Destinations module.</summary>
internal static class DestinationsAttachments
{
    public const string Module = "Destinations";
    public const string DestinationEntityType = "Destination";

    public static AttachmentOwner DestinationOwner(int destinationId) =>
        new(Module, DestinationEntityType, destinationId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Only image attachments can drive the gallery thumbnail or be marked as the
    /// primary image. The attachment subsystem stores canonical image content types.
    /// </summary>
    public static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
