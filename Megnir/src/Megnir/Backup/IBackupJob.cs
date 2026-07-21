namespace Megnir.Backup;

/// <summary>
/// Trabajo de backup one-shot de Megnir. Cada ejecución del proceso resuelve una
/// implementación y la ejecuta una sola vez.
/// </summary>
public interface IBackupJob
{
    /// <summary>
    /// Ejecuta el trabajo de backup una vez. Debe completar (o lanzar) para que el
    /// proceso one-shot termine con el código de salida correspondiente.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}
