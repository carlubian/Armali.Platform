using Megnir.Backup;
using Megnir.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Megnir.Tests;

public class AppRunnerTests
{
    /// <summary>Job que completa devolviendo un <see cref="BackupResult"/> fijo.</summary>
    private sealed class ResultJob : IBackupJob
    {
        private readonly BackupResult _result;

        public ResultJob(BackupResult result) => _result = result;

        public bool WasRun { get; private set; }

        public Task<BackupResult> RunAsync(CancellationToken cancellationToken)
        {
            WasRun = true;
            return Task.FromResult(_result);
        }
    }

    /// <summary>Job que siempre lanza (camino de fallo).</summary>
    private sealed class ThrowingJob : IBackupJob
    {
        public Task<BackupResult> RunAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("fallo simulado del job");
    }

    [Fact]
    public async Task RunAsync_returns_zero_on_success()
    {
        var job = new ResultJob(new BackupResult { Outcome = BackupOutcome.Success });

        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.Equal(0, exitCode);
        Assert.Equal(AppRunner.ExitSuccess, exitCode);
        Assert.True(job.WasRun);
    }

    [Fact]
    public async Task RunAsync_returns_two_on_partial()
    {
        var result = new BackupResult { Outcome = BackupOutcome.Partial };
        result.PartialErrors.Add(new BackupError("app-a", "/srv/app-a/data", "ruta inexistente"));
        var job = new ResultJob(result);

        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.Equal(2, exitCode);
        Assert.Equal(AppRunner.ExitPartial, exitCode);
        Assert.True(job.WasRun);
    }

    [Fact]
    public async Task RunAsync_returns_nonzero_and_does_not_propagate_on_failure()
    {
        var job = new ThrowingJob();

        // No debe propagar la excepción: la captura y la traduce a exit code.
        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(1, exitCode);
        Assert.Equal(AppRunner.ExitFailure, exitCode);
    }
}
