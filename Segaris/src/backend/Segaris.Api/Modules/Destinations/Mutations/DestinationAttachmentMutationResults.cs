using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Destinations.Mutations;

internal sealed record DestinationSetPrimaryResult(
    DestinationSetPrimaryOutcome Outcome,
    AttachmentDescriptor? Descriptor);

internal enum DestinationSetPrimaryOutcome
{
    Assigned,
    DestinationNotFound,
    AttachmentNotFound,
    NotImage,
}

internal enum DestinationDeleteAttachmentOutcome
{
    Deleted,
    DestinationNotFound,
    AttachmentNotFound,
}
