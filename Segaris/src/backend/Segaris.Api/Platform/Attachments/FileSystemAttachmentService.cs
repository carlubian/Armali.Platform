using Microsoft.EntityFrameworkCore;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Platform.Attachments;

internal sealed class FileSystemAttachmentService(
    SegarisDbContext dbContext,
    AttachmentStoragePaths paths,
    IClock clock,
    ILogger<FileSystemAttachmentService> logger) : IAttachmentService
{
    public async Task<AttachmentDescriptor> CreateAsync(
        AttachmentUpload upload,
        UserId createdBy,
        CancellationToken cancellationToken)
    {
        ValidateOwner(upload.Owner);
        var fileName = AttachmentPolicy.NormalizeAndValidateFileName(upload.FileName);
        var contentType = AttachmentPolicy.ValidateContentType(fileName, upload.ContentType);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var storageFileName = $"{Guid.NewGuid():N}{extension}";
        var temporaryPath = Path.Combine(paths.Staging, $"{Guid.NewGuid():N}.upload");
        var finalPath = paths.GetFilePath(upload.Owner.Module, storageFileName);

        Directory.CreateDirectory(paths.Staging);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        long size;
        try
        {
            size = await WriteBoundedAsync(upload.Content, temporaryPath, cancellationToken);
            await using (var validationStream = new FileStream(
                temporaryPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                AttachmentPolicy.ValidateContent(fileName, validationStream);
            }

            File.Move(temporaryPath, finalPath);
        }
        catch (ApiProblemException)
        {
            TryDelete(temporaryPath, "temporary upload");
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDelete(temporaryPath, "temporary upload");
            logger.LogError(exception, "Attachment file creation failed for module {Module}.", upload.Owner.Module);
            throw AttachmentProblem.StorageUnavailable();
        }

        var record = new AttachmentRecord
        {
            Module = AttachmentStoragePaths.NormalizeModule(upload.Owner.Module),
            EntityType = upload.Owner.EntityType,
            EntityId = upload.Owner.EntityId,
            OriginalFileName = fileName,
            StorageFileName = storageFileName,
            ContentType = contentType,
            Size = size,
            CreatedBy = createdBy.Value,
            CreatedAt = clock.UtcNow,
        };

        dbContext.Add(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryDelete(finalPath, "created attachment after database failure");
            throw;
        }

        return ToDescriptor(record);
    }

    public async Task<AttachmentDescriptor?> FindAsync(
        AttachmentId id,
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        var record = await FindRecordAsync(id, owner, tracking: false, cancellationToken);
        return record is null ? null : ToDescriptor(record);
    }

    public async Task<AttachmentDescriptor?> FindByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        var record = await FindRecordByOwnerAsync(owner, tracking: false, cancellationToken);
        return record is null ? null : ToDescriptor(record);
    }

    public async Task<IReadOnlyList<AttachmentDescriptor>> ListByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        ValidateOwner(owner);
        var module = AttachmentStoragePaths.NormalizeModule(owner.Module);
        var records = await dbContext.Set<AttachmentRecord>()
            .AsNoTracking()
            .Where(attachment => attachment.Module == module
                && attachment.EntityType == owner.EntityType
                && attachment.EntityId == owner.EntityId)
            // The auto-increment identifier is monotonic with creation, so ordering
            // by it reproduces upload order while avoiding SQLite's lack of ORDER BY
            // support for DateTimeOffset.
            .OrderBy(attachment => attachment.Id)
            .ToListAsync(cancellationToken);
        return records.Select(ToDescriptor).ToArray();
    }

    public async Task<AttachmentDownload?> OpenReadByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        var descriptor = await FindByOwnerAsync(owner, cancellationToken);
        return descriptor is null
            ? null
            : await OpenReadAsync(descriptor.Id, owner, cancellationToken);
    }

    public async Task<bool> DeleteByOwnerAsync(
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        var descriptor = await FindByOwnerAsync(owner, cancellationToken);
        return descriptor is not null
            && await DeleteAsync(descriptor.Id, owner, cancellationToken);
    }

    public async Task<AttachmentDownload?> OpenReadAsync(
        AttachmentId id,
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        var record = await FindRecordAsync(id, owner, tracking: false, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var path = paths.GetFilePath(record.Module, record.StorageFileName);
        try
        {
            var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return new AttachmentDownload(ToDescriptor(record), stream);
        }
        catch (FileNotFoundException)
        {
            logger.LogError("Attachment {AttachmentId} references missing file {StorageFileName}.", id.Value, record.StorageFileName);
            throw AttachmentProblem.StorageUnavailable();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Attachment {AttachmentId} could not be opened.", id.Value);
            throw AttachmentProblem.StorageUnavailable();
        }
    }

    public async Task<bool> DeleteAsync(
        AttachmentId id,
        AttachmentOwner owner,
        CancellationToken cancellationToken)
    {
        var record = await FindRecordAsync(id, owner, tracking: true, cancellationToken);
        if (record is null)
        {
            return false;
        }

        Directory.CreateDirectory(paths.Trash);
        var sourcePath = paths.GetFilePath(record.Module, record.StorageFileName);
        var trashPath = Path.Combine(paths.Trash, $"{Guid.NewGuid():N}.deleted");
        try
        {
            File.Move(sourcePath, trashPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Attachment {AttachmentId} file deletion could not begin.", id.Value);
            throw AttachmentProblem.StorageUnavailable();
        }

        dbContext.Remove(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            TryRestore(trashPath, sourcePath, id);
            throw;
        }

        try
        {
            File.Delete(trashPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Deleted attachment {AttachmentId} left a trash file for reconciliation.", id.Value);
        }

        return true;
    }

    private async Task<AttachmentRecord?> FindRecordAsync(
        AttachmentId id,
        AttachmentOwner owner,
        bool tracking,
        CancellationToken cancellationToken)
    {
        ValidateOwner(owner);
        var module = AttachmentStoragePaths.NormalizeModule(owner.Module);
        IQueryable<AttachmentRecord> query = dbContext.Set<AttachmentRecord>();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            attachment => attachment.Id == id.Value
                && attachment.Module == module
                && attachment.EntityType == owner.EntityType
                && attachment.EntityId == owner.EntityId,
            cancellationToken);
    }

    private async Task<AttachmentRecord?> FindRecordByOwnerAsync(
        AttachmentOwner owner,
        bool tracking,
        CancellationToken cancellationToken)
    {
        ValidateOwner(owner);
        var module = AttachmentStoragePaths.NormalizeModule(owner.Module);
        IQueryable<AttachmentRecord> query = dbContext.Set<AttachmentRecord>();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            attachment => attachment.Module == module
                && attachment.EntityType == owner.EntityType
                && attachment.EntityId == owner.EntityId,
            cancellationToken);
    }

    private static async Task<long> WriteBoundedAsync(
        Stream source,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[64 * 1024];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > AttachmentPolicy.MaximumFileSize)
            {
                throw new ApiProblemException(
                    StatusCodes.Status413PayloadTooLarge,
                    ApiErrorCodes.RequestTooLarge,
                    $"Attachments may not exceed {AttachmentPolicy.MaximumFileSize} bytes.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        await destination.FlushAsync(cancellationToken);
        return total;
    }

    private static void ValidateOwner(AttachmentOwner owner)
    {
        AttachmentStoragePaths.NormalizeModule(owner.Module);
        if (string.IsNullOrWhiteSpace(owner.EntityType) || owner.EntityType.Length > 80)
        {
            throw AttachmentProblem.Invalid("owner", "The owner entity type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(owner.EntityId) || owner.EntityId.Length > 120)
        {
            throw AttachmentProblem.Invalid("owner", "The owner entity identifier is invalid.");
        }
    }

    private static AttachmentDescriptor ToDescriptor(AttachmentRecord record) => new(
        new(record.Id),
        new(record.Module, record.EntityType, record.EntityId),
        record.OriginalFileName,
        record.ContentType,
        record.Size,
        new(record.CreatedBy),
        record.CreatedAt);

    private void TryDelete(string path, string purpose)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not remove {Purpose} at {Path}.", purpose, path);
        }
    }

    private void TryRestore(string source, string destination, AttachmentId id)
    {
        try
        {
            File.Move(source, destination);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(
                exception,
                "Attachment {AttachmentId} could not be restored after database deletion failed.",
                id.Value);
        }
    }
}
