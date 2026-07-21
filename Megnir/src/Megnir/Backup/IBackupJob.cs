namespace Megnir.Backup;

/// <summary>
/// Trabajo de backup one-shot de Megnir. Cada ejecución del proceso resuelve una
/// implementación y la ejecuta una sola vez.
/// </summary>
public interface IBackupJob
{
    /// <summary>
    /// Ejecuta el trabajo de backup una vez y devuelve el <see cref="BackupResult"/> con el
    /// desenlace (que <see cref="Megnir.Hosting.AppRunner"/> traduce a exit code). Debe
    /// completar (o lanzar) para que el proceso one-shot termine.
    /// </summary>
    Task<BackupResult> RunAsync(CancellationToken cancellationToken);
}
