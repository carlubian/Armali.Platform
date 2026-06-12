using System.Formats.Tar;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Platform.Attachments;
using Segaris.Api.Platform.Jobs;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Platform.Backup;

/// <summary>
/// Generates one restorable backup package in staging and atomically replaces the previous
/// latest package only after every artifact and the manifest succeed. Failure, cancellation,
/// and interruption remove staging data and leave the previous valid package untouched.
/// </summary>
internal sealed class BackupJobHandler(
    BackupPaths paths,
    IPostgresDumpRunner dumpRunner,
    AttachmentStoragePaths attachmentPaths,
    SegarisDbContext dbContext,
    IClock clock,
    ILogger<BackupJobHandler> logger) : IJobHandler
{
    public const string JobType = "backup";
    public const string ExclusivityKey = "backup";

    private const string DumpFileName = "database.dump";
    private const string AttachmentsDirectoryName = "attachments";
    private const string ManifestFileName = "manifest.json";

    private static readonly JsonSerializerOptions ManifestSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public async Task<JobResult> ExecuteAsync(
        JobExecutionContext context,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(paths.Root);
        Directory.CreateDirectory(paths.Staging);
        var stagingDirectory = paths.CreateStagingDirectory();
        var stagingArchive = paths.GetStagingArchivePath();

        try
        {
            await context.ReportProgressAsync(5, "starting", cancellationToken);

            var dumpPath = Path.Combine(stagingDirectory, DumpFileName);
            await dumpRunner.DumpAsync(dumpPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await context.ReportProgressAsync(45, "database_dumped", cancellationToken);

            CopyAttachments(stagingDirectory, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await context.ReportProgressAsync(70, "attachments_copied", cancellationToken);

            await WriteManifestAsync(stagingDirectory, cancellationToken);
            await context.ReportProgressAsync(85, "manifest_written", cancellationToken);

            TarFile.CreateFromDirectory(stagingDirectory, stagingArchive, includeBaseDirectory: false);
            cancellationToken.ThrowIfCancellationRequested();
            await context.ReportProgressAsync(95, "packaged", cancellationToken);

            // Atomic replace: renaming the completed archive over the previous package means
            // the latest file is only ever a complete, valid package.
            File.Move(stagingArchive, paths.PackagePath, overwrite: true);
            await context.ReportProgressAsync(100, "completed", cancellationToken);

            return new JobResult(BackupPaths.PackageFileName, "backup_completed");
        }
        finally
        {
            Cleanup(stagingDirectory, stagingArchive);
        }
    }

    private void CopyAttachments(string stagingDirectory, CancellationToken cancellationToken)
    {
        var attachmentsRoot = attachmentPaths.Root;
        if (!Directory.Exists(attachmentsRoot))
        {
            return;
        }

        var destinationRoot = Path.Combine(stagingDirectory, AttachmentsDirectoryName);
        foreach (var sourceFile in EnumerateLiveAttachmentFiles(attachmentsRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(attachmentsRoot, sourceFile);
            var destination = Path.Combine(destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(sourceFile, destination);
        }
    }

    private IEnumerable<string> EnumerateLiveAttachmentFiles(string attachmentsRoot)
    {
        return Directory.EnumerateFiles(attachmentsRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(attachmentPaths.Staging, StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith(attachmentPaths.Trash, StringComparison.OrdinalIgnoreCase));
    }

    private async Task WriteManifestAsync(string stagingDirectory, CancellationToken cancellationToken)
    {
        var files = new List<BackupFile>();
        foreach (var file in Directory.EnumerateFiles(stagingDirectory, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(stagingDirectory, file).Replace('\\', '/');
            files.Add(new BackupFile(relative, await ComputeHashAsync(file, cancellationToken), new FileInfo(file).Length));
        }

        var manifest = new BackupManifest(
            clock.UtcNow,
            ResolveApplicationVersion(),
            await ResolveSchemaVersionAsync(cancellationToken),
            files);

        var manifestPath = Path.Combine(stagingDirectory, ManifestFileName);
        await using var stream = new FileStream(
            manifestPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        await JsonSerializer.SerializeAsync(stream, manifest, ManifestSerializerOptions, cancellationToken);
    }

    private async Task<string> ResolveSchemaVersionAsync(CancellationToken cancellationToken)
    {
        var applied = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        return applied.LastOrDefault() ?? "none";
    }

    private static string ResolveApplicationVersion()
    {
        var assembly = typeof(BackupJobHandler).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }

    private void Cleanup(string stagingDirectory, string stagingArchive)
    {
        try
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Backup staging directory {Path} could not be removed.", stagingDirectory);
        }

        try
        {
            if (File.Exists(stagingArchive))
            {
                File.Delete(stagingArchive);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Backup staging archive {Path} could not be removed.", stagingArchive);
        }
    }
}
