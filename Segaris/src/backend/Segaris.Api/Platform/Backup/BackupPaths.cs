using Microsoft.Extensions.Options;
using Segaris.Api.Configuration;

namespace Segaris.Api.Platform.Backup;

/// <summary>
/// Resolves the backup output location. Only the latest completed package is retained, so
/// it has a stable filename and is replaced atomically. Staging artifacts live beneath a
/// hidden subdirectory and are removed on success, failure, or cancellation.
/// </summary>
internal sealed class BackupPaths
{
    /// <summary>The stable filename for the latest completed package.</summary>
    public const string PackageFileName = "segaris-backup.tar";

    public BackupPaths(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        var configuredPath = options.Value.BackupsPath;
        Root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Path.GetTempPath(), "segaris", "backups", environment.EnvironmentName)
            : configuredPath);
        Staging = Path.Combine(Root, ".staging");
        PackagePath = Path.Combine(Root, PackageFileName);
    }

    public string Root { get; }

    public string Staging { get; }

    public string PackagePath { get; }

    public string CreateStagingDirectory()
    {
        var path = Path.Combine(Staging, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetStagingArchivePath() =>
        Path.Combine(Staging, $"{Guid.NewGuid():N}.tar");
}
