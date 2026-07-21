using Megnir.Backup;
using Megnir.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Megnir.Tests;

public class AppRunnerTests
{
    /// <summary>Job que completa sin lanzar (camino feliz).</summary>
    private sealed class SucceedingJob : IBackupJob
    {
        public bool WasRun { get; private set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            WasRun = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>Job que siempre lanza (camino de fallo).</summary>
    private sealed class ThrowingJob : IBackupJob
    {
        public Task RunAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("fallo simulado del job");
    }

    [Fact]
    public async Task RunAsync_returns_zero_on_success()
    {
        var job = new SucceedingJob();

        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.Equal(0, exitCode);
        Assert.Equal(AppRunner.ExitSuccess, exitCode);
        Assert.True(job.WasRun);
    }

    [Fact]
    public async Task RunAsync_returns_nonzero_and_does_not_propagate_on_failure()
    {
        var job = new ThrowingJob();

        // No debe propagar la excepción: la captura y la traduce a exit code.
        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.NotEqual(0, exitCode);
        Assert.Equal(AppRunner.ExitFailure, exitCode);
    }
}
