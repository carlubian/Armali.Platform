namespace Segaris.Api.Platform.Backup;

/// <summary>
/// Produces a restorable PostgreSQL dump of the configured database. Backups are inherently
/// PostgreSQL-oriented, so this capability is only available when the active provider is
/// PostgreSQL.
/// </summary>
internal interface IPostgresDumpRunner
{
    Task DumpAsync(string outputPath, CancellationToken cancellationToken);
}
