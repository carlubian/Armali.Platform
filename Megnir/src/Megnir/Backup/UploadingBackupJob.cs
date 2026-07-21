using Microsoft.Extensions.Logging;

namespace Megnir.Backup;

/// <summary>
/// Ensambla el pipeline one-shot de H2: primero genera el backup local y, cuando existe
/// un artefacto, lo confirma en el destino remoto.
/// </summary>
/// <remarks>
/// Los errores de subida se dejan propagar deliberadamente. <see cref="Hosting.AppRunner"/>
/// los traduce a exit code 1 y el uploader nunca elimina el zip local, por lo que queda
/// disponible para diagnóstico o reintento manual. Si la subida funciona, se conserva el
/// desenlace Success/Partial devuelto por el trabajo local.
/// </remarks>
public sealed class UploadingBackupJob : IBackupJob
{
    private readonly LocalBackupJob _localBackupJob;
    private readonly IBackupUploader _uploader;
    private readonly ILogger<UploadingBackupJob> _logger;

    public UploadingBackupJob(
        LocalBackupJob localBackupJob,
        IBackupUploader uploader,
        ILogger<UploadingBackupJob> logger)
    {
        ArgumentNullException.ThrowIfNull(localBackupJob);
        ArgumentNullException.ThrowIfNull(uploader);
        ArgumentNullException.ThrowIfNull(logger);

        _localBackupJob = localBackupJob;
        _uploader = uploader;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BackupResult> RunAsync(CancellationToken cancellationToken)
    {
        var result = await _localBackupJob.RunAsync(cancellationToken).ConfigureAwait(false);

        if (result.Outcome == BackupOutcome.Failed || string.IsNullOrWhiteSpace(result.ZipPath))
        {
            throw new InvalidOperationException("El backup local no produjo un artefacto válido para subir.");
        }

        _logger.LogInformation(
            "Creación local confirmada: zip {Zip}, {Bytes} bytes, desenlace {Outcome}. Iniciando confirmación remota.",
            result.ZipPath,
            result.SizeBytes,
            result.Outcome);

        var uploaded = await _uploader.UploadAsync(result, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Confirmación remota completada: container={Container}, blob={BlobName}. Se conserva el desenlace local {Outcome}.",
            uploaded.Container,
            uploaded.BlobName,
            result.Outcome);

        return result;
    }
}
