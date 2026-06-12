namespace Segaris.Api.Platform.Backup;

/// <summary>
/// Describes the contents of a backup package. It contains no secrets or operational
/// configuration; attachment files are referenced by their UUID storage names, never by
/// their original filenames.
/// </summary>
internal sealed record BackupManifest(
    DateTimeOffset CreatedAt,
    string ApplicationVersion,
    string SchemaVersion,
    IReadOnlyList<BackupFile> Files);

internal sealed record BackupFile(string Path, string Sha256, long Size);
