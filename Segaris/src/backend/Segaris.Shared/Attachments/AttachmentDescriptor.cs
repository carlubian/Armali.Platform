using Segaris.Shared.Identity;

namespace Segaris.Shared.Attachments;

public sealed record AttachmentDescriptor(
    AttachmentId Id,
    AttachmentOwner Owner,
    string FileName,
    string ContentType,
    long Size,
    UserId CreatedBy,
    DateTimeOffset CreatedAt);
