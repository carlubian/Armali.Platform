using Megnir.Configuration;
using Microsoft.Extensions.Logging;

namespace Megnir.Backup;

/// <summary>
/// Copiador en caliente (Fase 1 de H1). Dada una <see cref="AppEntry"/> y un directorio
/// destino base (p. ej. <c>&lt;tmp&gt;/&lt;app&gt;/</c>), materializa recursivamente cada ruta de
/// <see cref="AppEntry.Paths"/> en un subdirectorio nombrado por el <em>basename</em> (último
/// segmento) de la ruta, aplicando la regla de layout del plan:
/// <list type="bullet">
///   <item>ruta de directorio → <c>&lt;destino&gt;/&lt;basename&gt;/…contenido…</c>;</item>
///   <item>ruta de fichero → el fichero directamente bajo <c>&lt;destino&gt;/</c>;</item>
///   <item>colisión de basenames dentro de la misma app → sufijo <c>-2</c>, <c>-3</c>, … y log.</item>
/// </list>
/// </summary>
/// <remarks>
/// Best-effort por fichero, sin reintento (decisión cerrada de H1): una ruta inexistente o un
/// fichero bloqueado/desaparecido se loguea, se registra como <see cref="BackupError"/> parcial y
/// no aborta la copia del resto. Los symlinks se siguen (se copia el contenido real del destino)
/// para respaldar los datos reales de los volúmenes. No ensambla el <see cref="BackupResult"/>
/// final: devuelve los errores parciales para que la Fase 3 los vuelque.
/// </remarks>
public sealed class HotFileCopier
{
    private readonly ILogger<HotFileCopier> _logger;

    public HotFileCopier(ILogger<HotFileCopier> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Copia recursivamente todas las rutas de <paramref name="app"/> dentro de
    /// <paramref name="destinationDirectory"/> aplicando la regla de layout por basename.
    /// </summary>
    /// <param name="app">Aplicación a respaldar (nombre + rutas de origen).</param>
    /// <param name="destinationDirectory">
    /// Directorio destino base para esta app (p. ej. <c>&lt;tmp&gt;/&lt;app&gt;/</c>). Se crea si no existe.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación cooperativa.</param>
    /// <returns>
    /// Lista (posiblemente vacía) de errores parciales acumulados durante la copia. Vacía = todas
    /// las rutas se copiaron sin incidencias.
    /// </returns>
    public IReadOnlyList<BackupError> CopyApp(
        AppEntry app,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrEmpty(destinationDirectory);

        var errors = new List<BackupError>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                ex,
                "app {App}: no se pudo crear el directorio destino {Destino}; se omiten sus rutas",
                app.Name, destinationDirectory);
            errors.Add(new BackupError(
                app.Name,
                destinationDirectory,
                $"no se pudo crear el directorio destino: {ex.Message}"));
            return errors;
        }

        foreach (var sourcePath in app.Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                _logger.LogWarning("app {App}: ruta de origen vacía; se omite", app.Name);
                errors.Add(new BackupError(app.Name, sourcePath ?? string.Empty, "ruta de origen vacía"));
                continue;
            }

            var isDirectory = Directory.Exists(sourcePath);
            var isFile = File.Exists(sourcePath);

            if (!isDirectory && !isFile)
            {
                _logger.LogWarning(
                    "app {App}: la ruta de origen {Ruta} no existe; se registra error parcial y se continúa",
                    app.Name, sourcePath);
                errors.Add(new BackupError(app.Name, sourcePath, "la ruta de origen no existe"));
                continue;
            }

            var targetName = ReserveUniqueName(BaseName(sourcePath), usedNames, app.Name, sourcePath);
            var targetPath = Path.Combine(destinationDirectory, targetName);

            if (isDirectory)
            {
                CopyDirectoryRecursive(app.Name, sourcePath, sourcePath, targetPath, errors, cancellationToken);
            }
            else
            {
                CopyFile(app.Name, sourcePath, sourcePath, targetPath, errors);
            }
        }

        return errors;
    }

    /// <summary>
    /// Reserva un nombre de destino único dentro de la app. Si el basename ya se usó, lo
    /// desambigua con sufijo <c>-2</c>, <c>-3</c>, … y lo registra en el log.
    /// </summary>
    private string ReserveUniqueName(
        string baseName,
        HashSet<string> usedNames,
        string appName,
        string sourcePath)
    {
        if (usedNames.Add(baseName))
        {
            return baseName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseName}-{suffix}";
            if (usedNames.Add(candidate))
            {
                _logger.LogInformation(
                    "app {App}: colisión de basename '{Base}' para la ruta {Ruta}; se desambigua como '{Nuevo}'",
                    appName, baseName, sourcePath, candidate);
                return candidate;
            }
        }
    }

    /// <summary>
    /// Copia recursivamente el contenido de <paramref name="currentSource"/> dentro de
    /// <paramref name="currentTarget"/>. Best-effort: los fallos de ficheros/subdirectorios
    /// concretos se registran y no abortan el resto.
    /// </summary>
    private void CopyDirectoryRecursive(
        string appName,
        string rootSource,
        string currentSource,
        string currentTarget,
        List<BackupError> errors,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Directory.CreateDirectory(currentTarget);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex,
                "app {App}: no se pudo crear el subdirectorio destino {Destino} (origen {Origen}); se salta",
                appName, currentTarget, currentSource);
            errors.Add(new BackupError(
                appName,
                currentSource,
                $"no se pudo crear el subdirectorio destino '{currentTarget}': {ex.Message}"));
            return;
        }

        // Ficheros directos de este nivel (los symlinks a fichero se siguen: File.Copy copia el
        // contenido del destino real).
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(currentSource);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex,
                "app {App}: no se pudo enumerar ficheros de {Origen}; se salta",
                appName, currentSource);
            errors.Add(new BackupError(appName, currentSource, $"no se pudieron enumerar ficheros: {ex.Message}"));
            files = Array.Empty<string>();
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileTarget = Path.Combine(currentTarget, Path.GetFileName(file));
            CopyFile(appName, rootSource, file, fileTarget, errors);
        }

        // Subdirectorios (los symlinks a directorio se siguen y se recorre su contenido).
        IEnumerable<string> subdirectories;
        try
        {
            subdirectories = Directory.EnumerateDirectories(currentSource);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                ex,
                "app {App}: no se pudo enumerar subdirectorios de {Origen}; se salta",
                appName, currentSource);
            errors.Add(new BackupError(appName, currentSource, $"no se pudieron enumerar subdirectorios: {ex.Message}"));
            subdirectories = Array.Empty<string>();
        }

        foreach (var subdirectory in subdirectories)
        {
            var subTarget = Path.Combine(currentTarget, Path.GetFileName(subdirectory));
            CopyDirectoryRecursive(appName, rootSource, subdirectory, subTarget, errors, cancellationToken);
        }
    }

    /// <summary>
    /// Copia un único fichero best-effort. Un fichero bloqueado o desaparecido durante la copia
    /// se loguea, se salta y se registra como error parcial.
    /// </summary>
    private void CopyFile(
        string appName,
        string sourceRoot,
        string sourceFile,
        string targetFile,
        List<BackupError> errors)
    {
        try
        {
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogWarning(
                ex,
                "app {App}: no se pudo copiar el fichero {Fichero} (bloqueado o desaparecido); se salta",
                appName, sourceFile);
            // Path apunta a la raíz configurada para que la Fase 3 atribuya el fallo a la ruta;
            // el fichero concreto queda en el mensaje.
            errors.Add(new BackupError(
                appName,
                sourceRoot,
                $"no se pudo copiar '{sourceFile}': {ex.Message}"));
        }
    }

    /// <summary>
    /// Último segmento (basename) de una ruta, robusto ante separadores <c>/</c> y <c>\</c> y ante
    /// una barra final. Si tras recortar no queda nombre (p. ej. una raíz), devuelve la ruta cruda.
    /// </summary>
    private static string BaseName(string path)
    {
        var trimmed = path.TrimEnd('/', '\\');
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(name) ? path : name;
    }
}
