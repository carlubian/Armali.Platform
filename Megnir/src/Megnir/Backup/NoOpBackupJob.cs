using Microsoft.Extensions.Logging;

namespace Megnir.Backup;

/// <summary>
/// Implementación placeholder de <see cref="IBackupJob"/> para el hito H0: no realiza
/// ninguna copia; solo deja constancia en el log de que el pipeline one-shot funciona.
/// Las implementaciones reales (copia local, subida a Azure, retención) llegan en H1–H3.
/// </summary>
public sealed class NoOpBackupJob : IBackupJob
{
    private readonly ILogger<NoOpBackupJob> _logger;

    public NoOpBackupJob(ILogger<NoOpBackupJob> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("job vacío OK");
        return Task.CompletedTask;
    }
}
