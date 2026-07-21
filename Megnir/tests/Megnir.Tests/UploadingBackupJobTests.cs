using Megnir.Backup;
using Megnir.Configuration;
using Megnir.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Megnir.Tests;

/// <summary>
/// Pruebas end-to-end del ensamblado H2 usando el pipeline local real y un uploader falso.
/// No requieren Azure ni una connection string.
/// </summary>
public class UploadingBackupJobTests
{
    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "megnir-upload-job-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string At(params string[] parts) => Path.Combine([Root, .. parts]);

        public string CreateFile(string content, params string[] parts)
        {
            var path = At(parts);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class FakeUploader(bool shouldThrow = false) : IBackupUploader
    {
        public BackupResult? Received { get; private set; }

        public Task<UploadedBackup> UploadAsync(BackupResult backup, CancellationToken cancellationToken = default)
        {
            Received = backup;
            if (shouldThrow)
            {
                throw new InvalidOperationException("fallo remoto simulado");
            }

            return Task.FromResult(new UploadedBackup("megnir", "host-a/" + Path.GetFileName(backup.ZipPath)));
        }
    }

    [Fact]
    public async Task RunAsync_local_and_remote_success_returns_success_and_uploads_zip()
    {
        using var workspace = new TempWorkspace();
        var uploader = new FakeUploader();
        var job = CreateJob(workspace, uploader, includeMissingPath: false);

        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.Equal(AppRunner.ExitSuccess, exitCode);
        var uploaded = Assert.IsType<BackupResult>(uploader.Received);
        Assert.Equal(BackupOutcome.Success, uploaded.Outcome);
        Assert.True(File.Exists(uploaded.ZipPath));
    }

    [Fact]
    public async Task RunAsync_local_partial_and_remote_success_preserves_partial_exit_code()
    {
        using var workspace = new TempWorkspace();
        var uploader = new FakeUploader();
        var job = CreateJob(workspace, uploader, includeMissingPath: true);

        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.Equal(AppRunner.ExitPartial, exitCode);
        var uploaded = Assert.IsType<BackupResult>(uploader.Received);
        Assert.Equal(BackupOutcome.Partial, uploaded.Outcome);
        Assert.True(File.Exists(uploaded.ZipPath));
    }

    [Fact]
    public async Task RunAsync_remote_failure_returns_failure_and_preserves_local_zip()
    {
        using var workspace = new TempWorkspace();
        var uploader = new FakeUploader(shouldThrow: true);
        var job = CreateJob(workspace, uploader, includeMissingPath: false);

        var exitCode = await AppRunner.RunAsync(job, NullLogger.Instance);

        Assert.Equal(AppRunner.ExitFailure, exitCode);
        var uploaded = Assert.IsType<BackupResult>(uploader.Received);
        Assert.True(File.Exists(uploaded.ZipPath));
        Assert.Equal(Path.GetFullPath(workspace.At("out")), Path.GetFullPath(Path.GetDirectoryName(uploaded.ZipPath)!));
    }

    private static UploadingBackupJob CreateJob(TempWorkspace workspace, IBackupUploader uploader, bool includeMissingPath)
    {
        var source = workspace.CreateFile("backup data", "source", "app-a", "data.txt");
        var options = new MegnirOptions
        {
            TempDirectory = workspace.At("temp"),
            OutputDirectory = workspace.At("out"),
            SizeWarningMb = 100,
            Apps =
            {
                new AppEntry
                {
                    Name = "app-a",
                    Paths = includeMissingPath
                        ? [Path.GetDirectoryName(source)!, workspace.At("missing")]
                        : [Path.GetDirectoryName(source)!],
                },
            },
        };

        var localJob = new LocalBackupJob(
            NullLogger<LocalBackupJob>.Instance,
            Options.Create(options),
            new HotFileCopier(NullLogger<HotFileCopier>.Instance),
            new BackupArchiver(NullLogger<BackupArchiver>.Instance));

        return new UploadingBackupJob(localJob, uploader, NullLogger<UploadingBackupJob>.Instance);
    }
}
