using Megnir.Backup;
using Microsoft.Extensions.Logging;

namespace Megnir.Hosting;

/// <summary>
/// Orquesta la ejecución one-shot del proceso: ejecuta el <see cref="IBackupJob"/> una
/// única vez, captura cualquier fallo y traduce el resultado a un código de salida
/// (0 = éxito, ≠ 0 = fallo) apto para systemd/monitorización (RNF4).
/// </summary>
/// <remarks>
/// Se extrae de <c>Program.cs</c> para poder testear ambos caminos (feliz y fallo) sin
/// arrancar el proceso: nunca propaga la excepción del job, la convierte en exit code.
/// </remarks>
public static class AppRunner
{
    /// <summary>Código de salida de éxito.</summary>
    public const int ExitSuccess = 0;

    /// <summary>Código de salida ante un fallo no controlado del job.</summary>
    public const int ExitFailure = 1;

    /// <summary>
    /// Ejecuta el <paramref name="job"/> una vez y devuelve el código de salida.
    /// Cualquier excepción se captura y loguea; el método nunca la propaga.
    /// </summary>
    public static async Task<int> RunAsync(
        IBackupJob job,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogInformation("Ejecutando trabajo de backup (one-shot).");
        try
        {
            await job.RunAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Trabajo de backup completado con éxito (exit code {ExitCode}).", ExitSuccess);
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "Trabajo de backup fallido (exit code {ExitCode}).", ExitFailure);
            return ExitFailure;
        }
    }
}
