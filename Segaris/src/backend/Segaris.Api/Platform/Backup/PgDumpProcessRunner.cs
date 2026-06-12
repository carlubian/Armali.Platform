using System.Diagnostics;
using System.Text;
using Npgsql;
using Segaris.Api.Platform.Jobs;

namespace Segaris.Api.Platform.Backup;

/// <summary>
/// Runs the external <c>pg_dump</c> tool to produce a custom-format dump. The password is
/// passed through the <c>PGPASSWORD</c> environment variable so it never appears in the
/// process command line or in logs.
/// </summary>
internal sealed class PgDumpProcessRunner(
    IConfiguration configuration,
    ILogger<PgDumpProcessRunner> logger) : IPostgresDumpRunner
{
    public async Task DumpAsync(string outputPath, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Segaris")
            ?? throw new JobFailureException("backup_misconfigured", "No Segaris connection string is configured.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pg_dump",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--format=custom");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--no-privileges");
        startInfo.ArgumentList.Add($"--file={outputPath}");
        startInfo.ArgumentList.Add($"--host={builder.Host}");
        startInfo.ArgumentList.Add($"--port={(builder.Port == 0 ? 5432 : builder.Port)}");
        startInfo.ArgumentList.Add($"--username={builder.Username}");
        startInfo.ArgumentList.Add($"--dbname={builder.Database}");
        startInfo.Environment["PGPASSWORD"] = builder.Password ?? string.Empty;

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new JobFailureException("backup_pg_dump_unavailable", "pg_dump could not be started.");
            }
        }
        catch (Exception exception) when (exception is not JobFailureException)
        {
            logger.LogError(exception, "pg_dump could not be launched. Is the PostgreSQL client installed?");
            throw new JobFailureException("backup_pg_dump_unavailable", "pg_dump is not available on this host.");
        }

        var stderr = new StringBuilder();
        var errorReader = ReadAllAsync(process.StandardError, stderr, cancellationToken);
        var outputReader = ReadAllAsync(process.StandardOutput, null, cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        await errorReader;
        await outputReader;

        if (process.ExitCode != 0)
        {
            // The captured stderr can name database objects; keep it in logs only.
            logger.LogError(
                "pg_dump exited with code {ExitCode}: {Error}",
                process.ExitCode,
                stderr.ToString());
            throw new JobFailureException("backup_pg_dump_failed", "The database dump failed.");
        }
    }

    private static async Task ReadAllAsync(
        StreamReader reader,
        StringBuilder? sink,
        CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        while (true)
        {
            var read = await reader.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            sink?.Append(buffer, 0, read);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
