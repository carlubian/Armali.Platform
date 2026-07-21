using Megnir.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Megnir.Backup;

/// <summary>
/// Implementación real de <see cref="IBackupJob"/> para el hito H1 (Fase 3): orquesta las piezas
/// aisladas de las fases anteriores en el pipeline de backup local one-shot. Por cada
/// <see cref="AppEntry"/> configurada invoca el copiador en caliente (<see cref="HotFileCopier"/>,
/// Fase 1) sobre un directorio de trabajo temporal único, comprime el árbol resultante a un
/// <c>.zip</c> agrupado por app (<see cref="BackupArchiver"/>, Fase 2) en el
/// <see cref="MegnirOptions.OutputDirectory"/>, y ensambla el <see cref="BackupResult"/> global.
/// </summary>
/// <remarks>
/// <para>
/// El directorio temporal de trabajo (<c>&lt;tempBase&gt;/megnir-&lt;guid&gt;</c>) se limpia siempre en un
/// <c>finally</c> (RNF2): si la limpieza falla se loguea un <see cref="LogLevel.Warning"/> y el
/// desenlace <b>no</b> cambia por ello. La copia es best-effort (decisión cerrada de H1): los
/// errores por ruta/app se acumulan sin abortar; si el <c>.zip</c> se genera pero hubo algún error
/// parcial el desenlace es <see cref="BackupOutcome.Partial"/> (exit 2), y
/// <see cref="BackupOutcome.Success"/> (exit 0) si no hubo ninguno.
/// </para>
/// <para>
/// Si <see cref="BackupArchiver.CreateArchive"/> lanza (p. ej. no hay nada que comprimir o error
/// irrecuperable) la excepción se deja propagar: <see cref="Megnir.Hosting.AppRunner"/> la captura y
/// la mapea a exit <c>1</c> (fallo total). No se traga aquí.
/// </para>
/// </remarks>
public sealed class LocalBackupJob : IBackupJob
{
    private const string WorkingDirPrefix = "megnir-";

    private readonly ILogger<LocalBackupJob> _logger;
    private readonly MegnirOptions _options;
    private readonly HotFileCopier _copier;
    private readonly BackupArchiver _archiver;

    public LocalBackupJob(
        ILogger<LocalBackupJob> logger,
        IOptions<MegnirOptions> options,
        HotFileCopier copier,
        BackupArchiver archiver)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(copier);
        ArgumentNullException.ThrowIfNull(archiver);

        _logger = logger;
        _options = options.Value;
        _copier = copier;
        _archiver = archiver;
    }

    /// <inheritdoc />
    public Task<BackupResult> RunAsync(CancellationToken cancellationToken)
    {
        // El pipeline es síncrono (I/O de ficheros); se envuelve en Task para respetar el contrato.
        return Task.FromResult(Run(cancellationToken));
    }

    private BackupResult Run(CancellationToken cancellationToken)
    {
        var tempBase = string.IsNullOrWhiteSpace(_options.TempDirectory)
            ? Path.GetTempPath()
            : _options.TempDirectory;

        // Temporal único por ejecución para que dos ejecuciones simultáneas no colisionen (RNF2).
        var workingDirectory = Path.Combine(tempBase, WorkingDirPrefix + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workingDirectory);

            var partialErrors = new List<BackupError>();

            foreach (var app in _options.Apps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var appDirectory = Path.Combine(workingDirectory, app.Name);
                var appErrors = _copier.CopyApp(app, appDirectory, cancellationToken);
                partialErrors.AddRange(appErrors);
            }

            // Si esto lanza (nada que comprimir / error irrecuperable) se deja propagar: AppRunner → exit 1.
            var archive = _archiver.CreateArchive(
                workingDirectory, _options.OutputDirectory, _options.SizeWarningMb, cancellationToken);

            var result = new BackupResult
            {
                Outcome = partialErrors.Count > 0 ? BackupOutcome.Partial : BackupOutcome.Success,
                ZipPath = archive.ZipPath,
                SizeBytes = archive.SizeBytes,
            };
            foreach (var error in partialErrors)
            {
                result.PartialErrors.Add(error);
            }

            LogSummary(result);
            return result;
        }
        finally
        {
            CleanupWorkingDirectory(workingDirectory);
        }
    }

    /// <summary>
    /// Resumen de la ejecución: nº de apps, rutas OK vs fallidas, ruta y tamaño del <c>.zip</c> y el
    /// desenlace. Las rutas totales son la suma de <see cref="AppEntry.Paths"/>; las fallidas se
    /// estiman por el nº de errores parciales acumulados.
    /// </summary>
    private void LogSummary(BackupResult result)
    {
        var appCount = _options.Apps.Count;
        var totalPaths = _options.Apps.Sum(a => a.Paths.Count);
        var failedPaths = result.PartialErrors.Count;
        var okPaths = Math.Max(0, totalPaths - failedPaths);

        _logger.LogInformation(
            "Resumen del backup: {AppCount} app(s), rutas OK {OkPaths}/{TotalPaths} (fallidas {FailedPaths}), " +
            "zip {Zip} ({Bytes} bytes), desenlace {Outcome}.",
            appCount, okPaths, totalPaths, failedPaths, result.ZipPath, result.SizeBytes, result.Outcome);
    }

    /// <summary>
    /// Borra recursivamente el directorio de trabajo. Un fallo de limpieza se loguea como warning y
    /// no altera el desenlace ni el exit code (RNF2).
    /// </summary>
    private void CleanupWorkingDirectory(string workingDirectory)
    {
        try
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex,
                "no se pudo limpiar el directorio temporal de trabajo {Temp}; se continúa sin alterar el exit code",
                workingDirectory);
        }
    }
}
