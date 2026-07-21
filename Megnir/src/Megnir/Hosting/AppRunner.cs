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
    /// <summary>Código de salida de éxito total.</summary>
    public const int ExitSuccess = 0;

    /// <summary>Código de salida ante un fallo no controlado del job.</summary>
    public const int ExitFailure = 1;

    /// <summary>
    /// Código de salida ante un backup parcial: el <c>.zip</c> se generó pero alguna
    /// ruta/fichero falló (RNF4 ≠ 0, distinguible del éxito y del fallo total).
    /// </summary>
    public const int ExitPartial = 2;

    /// <summary>
    /// Ejecuta el <paramref name="job"/> una vez y traduce su <see cref="BackupResult"/> a
    /// un código de salida (Success→0, Partial→2). Cualquier excepción se captura y loguea
    /// como fallo no controlado (→1); el método nunca la propaga.
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
            var result = await job.RunAsync(cancellationToken).ConfigureAwait(false);
            var exitCode = MapOutcome(result.Outcome);

            switch (result.Outcome)
            {
                case BackupOutcome.Partial:
                    logger.LogWarning(
                        "Trabajo de backup completado de forma parcial: {ErrorCount} error(es) parcial(es) (exit code {ExitCode}).",
                        result.PartialErrors.Count, exitCode);
                    break;
                default:
                    logger.LogInformation(
                        "Trabajo de backup completado con éxito (exit code {ExitCode}).", exitCode);
                    break;
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "Trabajo de backup fallido (exit code {ExitCode}).", ExitFailure);
            return ExitFailure;
        }
    }

    /// <summary>
    /// Traduce el desenlace del backup a exit code: <see cref="BackupOutcome.Success"/>→0,
    /// <see cref="BackupOutcome.Partial"/>→2. Un <see cref="BackupOutcome.Failed"/> devuelto
    /// (sin excepción) se trata también como fallo no controlado (→1).
    /// </summary>
    private static int MapOutcome(BackupOutcome outcome) => outcome switch
    {
        BackupOutcome.Success => ExitSuccess,
        BackupOutcome.Partial => ExitPartial,
        _ => ExitFailure,
    };
}
