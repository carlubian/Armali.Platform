namespace Segaris.Shared.Attachments;

public sealed record AttachmentUpload(
    AttachmentOwner Owner,
    string FileName,
    string ContentType,
    Stream Content);
