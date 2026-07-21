using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Megnir.Backup;

/// <summary>
/// Empaquetador de backups (Fase 2 de H1). Dado un <em>directorio de trabajo ya poblado</em>
/// (el árbol <c>&lt;tmp&gt;</c> con un subdirectorio por app tal como lo deja la Fase 1) lo comprime a
/// un único <c>.zip</c> con <see cref="System.IO.Compression"/>, preservando <c>&lt;app&gt;/…</c> como
/// raíces del comprimido, valida su tamaño y lo entrega en el directorio de salida.
/// </summary>
/// <remarks>
/// <para>
/// Contrato de la fase (no ensambla el <see cref="BackupResult"/> final ni toca exit codes; eso es
/// la Fase 3):
/// </para>
/// <list type="number">
///   <item>
///     <b>Compresión:</b> el nombre del artefacto es
///     <c>megnir-backup-&lt;yyyyMMdd-HHmmss&gt;.zip</c>. El instante se toma de un reloj inyectable
///     (<see cref="Func{TResult}"/> de <see cref="DateTimeOffset"/>) para poder testear el naming
///     de forma determinista. Las entradas del zip usan siempre el separador <c>/</c>.
///   </item>
///   <item>
///     <b>Escritura atómica (RNF2):</b> se comprime primero a un fichero temporal
///     <c>&lt;final&gt;.tmp</c> dentro del propio <see cref="MegnirOptions.OutputDirectory"/> (mismo
///     volumen ⇒ el <see cref="File.Move(string, string)"/> final es un rename atómico) y solo
///     entonces se renombra al nombre definitivo. Nunca queda un <c>.zip</c> a medias. El
///     <c>OutputDirectory</c> se crea si no existe.
///   </item>
///   <item>
///     <b>Validación de tamaño:</b> si el <c>.zip</c> final supera <c>SizeWarningMb</c> se emite un
///     <see cref="LogLevel.Warning"/>. No aborta ni cambia nada más (decisión cerrada: solo aviso).
///   </item>
/// </list>
/// <para>
/// La limpieza del directorio de trabajo <c>&lt;tmp&gt;</c> es responsabilidad de la Fase 3; aquí solo
/// se limpia el <c>.tmp</c> intermedio propio de la compresión si algo falla a mitad.
/// </para>
/// </remarks>
public sealed class BackupArchiver
{
    private const string ZipNamePrefix = "megnir-backup-";
    private const string ZipExtension = ".zip";
    private const string TempSuffix = ".tmp";

    private readonly ILogger<BackupArchiver> _logger;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// Crea el empaquetador. Usa <see cref="DateTimeOffset.Now"/> como reloj para el naming; para
    /// tests deterministas inyecta <paramref name="clock"/>.
    /// </summary>
    /// <param name="logger">Logger para los avisos (p. ej. exceso de tamaño).</param>
    /// <param name="clock">
    /// Reloj para el timestamp del nombre del artefacto. Si es <c>null</c>, se usa
    /// <see cref="DateTimeOffset.Now"/>.
    /// </param>
    public BackupArchiver(ILogger<BackupArchiver> logger, Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    /// <summary>
    /// Comprime <paramref name="workingDirectory"/> a un <c>.zip</c>, lo valida y lo deja en
    /// <paramref name="outputDirectory"/> de forma atómica.
    /// </summary>
    /// <param name="workingDirectory">
    /// Directorio de trabajo ya poblado (árbol con un subdirectorio por app). Su contenido se
    /// coloca en la raíz del zip (cada app como directorio padre).
    /// </param>
    /// <param name="outputDirectory">
    /// Directorio de salida donde queda el <c>.zip</c> final. Se crea si no existe.
    /// </param>
    /// <param name="sizeWarningMb">
    /// Umbral (en MB) por encima del cual se emite un warning por el log. Solo aviso: no aborta.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación cooperativa.</param>
    /// <returns>Ruta absoluta del <c>.zip</c> generado y su tamaño en bytes.</returns>
    public ArchiveResult CreateArchive(
        string workingDirectory,
        string outputDirectory,
        int sizeWarningMb,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException(
                $"el directorio de trabajo a comprimir no existe: {workingDirectory}");
        }

        Directory.CreateDirectory(outputDirectory);

        var fileName = ZipNamePrefix + _clock().ToString("yyyyMMdd-HHmmss") + ZipExtension;
        var finalPath = Path.GetFullPath(Path.Combine(outputDirectory, fileName));
        // El temporal vive en el MISMO directorio (mismo volumen) para que el Move final sea un
        // rename atómico y no un copy+delete entre volúmenes.
        var tempPath = finalPath + TempSuffix;

        try
        {
            CompressTo(tempPath, workingDirectory, cancellationToken);

            var sizeBytes = new FileInfo(tempPath).Length;

            // Rename atómico al nombre definitivo: hasta este punto no existe ningún .zip visible.
            File.Move(tempPath, finalPath);

            WarnIfOversized(finalPath, sizeBytes, sizeWarningMb);

            _logger.LogInformation(
                "archivo de backup generado: {Zip} ({Bytes} bytes)", finalPath, sizeBytes);

            return new ArchiveResult(finalPath, sizeBytes);
        }
        finally
        {
            // Si algo falló antes del Move, limpiamos el .tmp intermedio para no dejar basura.
            TryDeleteLeftoverTemp(tempPath);
        }
    }

    /// <summary>
    /// Crea el zip en <paramref name="destinationPath"/> con una entrada por fichero del árbol,
    /// usando rutas relativas al <paramref name="workingDirectory"/> normalizadas a <c>/</c>.
    /// </summary>
    private void CompressTo(string destinationPath, string workingDirectory, CancellationToken cancellationToken)
    {
        var rootFull = Path.GetFullPath(workingDirectory);

        using var archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create);

        foreach (var file in Directory.EnumerateFiles(rootFull, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(rootFull, file);
            // Las entradas del zip usan siempre '/' (separador del formato), también en Windows.
            var entryName = relative.Replace('\\', '/');
            archive.CreateEntryFromFile(file, entryName);
        }
    }

    /// <summary>Emite un warning si el tamaño del <c>.zip</c> supera el umbral en MB.</summary>
    private void WarnIfOversized(string zipPath, long sizeBytes, int sizeWarningMb)
    {
        if (sizeWarningMb <= 0)
        {
            // Un umbral 0 o negativo se interpreta como "avisar de cualquier tamaño no vacío".
            if (sizeBytes > 0)
            {
                _logger.LogWarning(
                    "el archivo de backup {Zip} ({Bytes} bytes) supera el umbral de aviso ({Umbral} MB)",
                    zipPath, sizeBytes, sizeWarningMb);
            }
            return;
        }

        var thresholdBytes = (long)sizeWarningMb * 1024 * 1024;
        if (sizeBytes > thresholdBytes)
        {
            _logger.LogWarning(
                "el archivo de backup {Zip} ({Bytes} bytes) supera el umbral de aviso ({Umbral} MB); " +
                "la ejecución continúa (solo aviso)",
                zipPath, sizeBytes, sizeWarningMb);
        }
    }

    private void TryDeleteLeftoverTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex, "no se pudo limpiar el fichero temporal de compresión {Temp}", tempPath);
        }
    }
}

/// <summary>
/// Datos que la Fase 2 entrega a la Fase 3 para rellenar el <see cref="BackupResult"/>: la ruta del
/// <c>.zip</c> generado y su tamaño en bytes.
/// </summary>
/// <param name="ZipPath">Ruta absoluta del <c>.zip</c> final en el directorio de salida.</param>
/// <param name="SizeBytes">Tamaño del <c>.zip</c> en bytes.</param>
public readonly record struct ArchiveResult(string ZipPath, long SizeBytes);
