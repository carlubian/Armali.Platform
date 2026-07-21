namespace Megnir.Backup;

/// <summary>
/// Confirma la copia de un artefacto local de backup en el almacenamiento remoto.
/// La implementación de Azure se incorpora en H2/Fase 1.
/// </summary>
public interface IBackupUploader
{
    /// <summary>Sube el artefacto y devuelve la identidad del blob confirmado.</summary>
    Task<UploadedBackup> UploadAsync(BackupResult backup, CancellationToken cancellationToken = default);
}

/// <summary>Identidad del blob que el destino remoto ha confirmado.</summary>
public sealed record UploadedBackup(string Container, string BlobName);
