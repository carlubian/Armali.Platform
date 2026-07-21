using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Megnir.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Megnir.Backup;

/// <summary>
/// Sube el artefacto local a Azure Blob Storage sin sobrescribir copias existentes.
/// </summary>
public sealed class AzureBlobBackupUploader : IBackupUploader
{
    private readonly BlobServiceClient _serviceClient;
    private readonly ResolvedAzureOptions _options;
    private readonly ILogger<AzureBlobBackupUploader> _logger;

    /// <summary>Construye el cliente exclusivamente desde la connection string configurada.</summary>
    public AzureBlobBackupUploader(
        IOptions<MegnirOptions> options,
        ILogger<AzureBlobBackupUploader> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = AzureOptionsValidator.ValidateAndResolve(options.Value.Azure);
        _serviceClient = new BlobServiceClient(options.Value.Azure.ConnectionString);
        _logger = logger;
    }

    /// <summary>
    /// Constructor para pruebas: recibe un cliente con transporte simulado y opciones ya validadas.
    /// </summary>
    public AzureBlobBackupUploader(
        BlobServiceClient serviceClient,
        ResolvedAzureOptions options,
        ILogger<AzureBlobBackupUploader> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceClient = serviceClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UploadedBackup> UploadAsync(BackupResult backup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);

        if (backup.Outcome == BackupOutcome.Failed || string.IsNullOrWhiteSpace(backup.ZipPath))
        {
            throw new InvalidOperationException("No hay un artefacto de backup v\u00e1lido para subir.");
        }

        var fileInfo = new FileInfo(backup.ZipPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("No se encontr\u00f3 el artefacto de backup para subir.", backup.ZipPath);
        }

        var blobName = $"{_options.HostPrefix}/{fileInfo.Name}";
        var containerClient = _serviceClient.GetBlobContainerClient(_options.Container);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var blobClient = containerClient.GetBlobClient(blobName);
        await using var input = new FileStream(
            fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

        await blobClient.UploadAsync(
                input,
                new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                },
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Backup confirmado en Azure: container={Container}, prefix={Prefix}, name={Name}, bytes={Bytes}.",
            _options.Container, _options.HostPrefix, fileInfo.Name, fileInfo.Length);

        return new UploadedBackup(_options.Container, blobName);
    }
}
