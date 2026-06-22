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
}
