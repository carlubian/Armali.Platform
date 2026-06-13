using Segaris.Shared.Identity;

namespace Segaris.Shared.Attachments;

public interface IAttachmentService
{
    Task<AttachmentDescriptor> CreateAsync(
        AttachmentUpload upload,
        UserId createdBy,
        CancellationToken cancellationToken);

    Task<AttachmentDescriptor?> FindAsync(
        AttachmentId id,
        AttachmentOwner owner,
        CancellationToken cancellationToken);

    Task<AttachmentDescriptor?> FindByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken);

    Task<AttachmentDownload?> OpenReadAsync(
        AttachmentId id,
        AttachmentOwner owner,
        CancellationToken cancellationToken);

    Task<AttachmentDownload?> OpenReadByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        AttachmentId id,
        AttachmentOwner owner,
        CancellationToken cancellationToken);

    Task<bool> DeleteByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken);
}
