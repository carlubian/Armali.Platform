using Microsoft.EntityFrameworkCore;
using Segaris.Persistence;

namespace Segaris.Api.Platform.Attachments;

internal sealed class AttachmentReconciliationService(
    SegarisDbContext dbContext,
    AttachmentStoragePaths paths)
{
    public async Task<AttachmentReconciliationResult> InspectAsync(CancellationToken cancellationToken)
    {
        var records = await dbContext.Set<AttachmentRecord>()
            .AsNoTracking()
            .Select(attachment => new { attachment.Module, attachment.StorageFileName })
            .ToListAsync(cancellationToken);
        var referenced = records
            .Select(record => Path.GetFullPath(paths.GetFilePath(record.Module, record.StorageFileName)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = referenced.Count(path => !File.Exists(path));

        var physical = Directory.Exists(paths.Root)
            ? Directory.EnumerateFiles(paths.Root, "*", SearchOption.AllDirectories)
                .Where(path => !path.StartsWith(paths.Staging, StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith(paths.Trash, StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFullPath)
                .ToArray()
            : [];
        var unreferenced = physical.Count(path => !referenced.Contains(path));
        var staging = Directory.Exists(paths.Staging)
            ? Directory.EnumerateFiles(paths.Staging).Count()
            : 0;
        var trash = Directory.Exists(paths.Trash)
            ? Directory.EnumerateFiles(paths.Trash).Count()
            : 0;

        return new(missing, unreferenced, staging, trash);
    }
}

internal sealed record AttachmentReconciliationResult(
    int MissingFiles,
    int UnreferencedFiles,
    int StagingFiles,
    int TrashFiles);
