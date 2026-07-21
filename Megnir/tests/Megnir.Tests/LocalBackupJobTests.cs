using System.IO.Compression;
using Megnir.Backup;
using Megnir.Configuration;
using Megnir.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Megnir.Tests;

/// <summary>
/// Validación end-to-end del hito H1: el <see cref="LocalBackupJob"/> orquesta copia en caliente +
/// compresión + limpieza y produce un <c>.zip</c> con la estructura de directorios padre por app,
/// dejando el temporal de trabajo limpio y devolviendo el desenlace correcto (Success / Partial).
/// </summary>
public class LocalBackupJobTests
{
    /// <summary>Área de trabajo temporal autolimpiante para las fixtures del test.</summary>
    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; }

        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "megnir-job-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string At(params string[] parts) =>
            Path.Combine(new[] { Root }.Concat(parts).ToArray());

        public string CreateDir(params string[] parts)
        {
            var p = At(parts);
            Directory.CreateDirectory(p);
            return p;
        }

        public string CreateFile(string content, params string[] parts)
        {
            var p = At(parts);
            Directory.CreateDirectory(Path.GetDirectoryName(p)!);
            File.WriteAllText(p, content);
            return p;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // Best-effort: no romper la suite si el SO retiene un handle.
            }
        }
    }

    private static LocalBackupJob CreateJob(MegnirOptions options) =>
        new(
            NullLogger<LocalBackupJob>.Instance,
            Options.Create(options),
            new HotFileCopier(NullLogger<HotFileCopier>.Instance),
            new BackupArchiver(NullLogger<BackupArchiver>.Instance));

    [Fact]
    public async Task RunAsync_happy_path_produces_zip_grouped_by_app_and_cleans_temp()
    {
        using var ws = new TempWorkspace();

        // Fixtures de dos apps con contenido real.
        ws.CreateFile("a-data", "src", "app-a", "data", "f1.txt");
        ws.CreateFile("a-config", "src", "app-a", "config", "g.txt");
        ws.CreateFile("b-lib", "src", "app-b", "lib", "h.txt");

        var tempBase = ws.CreateDir("tempbase");
        var output = ws.At("out");

        var options = new MegnirOptions
        {
            TempDirectory = tempBase,
            OutputDirectory = output,
            SizeWarningMb = 100,
            Apps =
            {
                new AppEntry { Name = "app-a", Paths = { ws.At("src", "app-a", "data"), ws.At("src", "app-a", "config") } },
                new AppEntry { Name = "app-b", Paths = { ws.At("src", "app-b", "lib") } },
            },
        };

        var result = await CreateJob(options).RunAsync(CancellationToken.None);

        // (4) camino feliz => Success.
        Assert.Equal(BackupOutcome.Success, result.Outcome);
        Assert.Empty(result.PartialErrors);

        // (1) el .zip existe en OutputDirectory.
        Assert.True(File.Exists(result.ZipPath));
        Assert.Equal(Path.GetFullPath(output), Path.GetFullPath(Path.GetDirectoryName(result.ZipPath)!));
        Assert.Equal(new FileInfo(result.ZipPath).Length, result.SizeBytes);

        // (2) estructura de directorios padre por app, verificada por FullName con '/'.
        using (var zip = ZipFile.OpenRead(result.ZipPath))
        {
            var names = zip.Entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            Assert.Equal(
                new[]
                {
                    "app-a/config/g.txt",
                    "app-a/data/f1.txt",
                    "app-b/lib/h.txt",
                },
                names);
            Assert.All(names, n => Assert.DoesNotContain('\\', n));
        }

        // (3) el temporal de trabajo quedó limpio: no queda ningún megnir-<guid> en el tempBase.
        Assert.Empty(Directory.GetDirectories(tempBase, "megnir-*"));
    }

    [Fact]
    public async Task RunAsync_partial_path_missing_still_produces_zip_and_returns_partial()
    {
        using var ws = new TempWorkspace();

        ws.CreateFile("a-data", "src", "app-a", "data", "f1.txt");
        ws.CreateFile("b-lib", "src", "app-b", "lib", "h.txt");
        var missing = ws.At("src", "app-a", "does-not-exist");

        var tempBase = ws.CreateDir("tempbase");
        var output = ws.At("out");

        var options = new MegnirOptions
        {
            TempDirectory = tempBase,
            OutputDirectory = output,
            SizeWarningMb = 100,
            Apps =
            {
                new AppEntry { Name = "app-a", Paths = { ws.At("src", "app-a", "data"), missing } },
                new AppEntry { Name = "app-b", Paths = { ws.At("src", "app-b", "lib") } },
            },
        };

        var job = CreateJob(options);
        var result = await job.RunAsync(CancellationToken.None);

        // El .zip se genera igualmente con el resto y el desenlace es Partial.
        Assert.Equal(BackupOutcome.Partial, result.Outcome);
        Assert.True(File.Exists(result.ZipPath));

        var error = Assert.Single(result.PartialErrors);
        Assert.Equal("app-a", error.App);
        Assert.Equal(missing, error.Path);

        // El resto del contenido sí está en el zip.
        using (var zip = ZipFile.OpenRead(result.ZipPath))
        {
            var names = zip.Entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
            Assert.Equal(
                new[]
                {
                    "app-a/data/f1.txt",
                    "app-b/lib/h.txt",
                },
                names);
        }

        // El temporal quedó limpio pese al fallo parcial.
        Assert.Empty(Directory.GetDirectories(tempBase, "megnir-*"));

        // Mapeado vía AppRunner, el desenlace parcial es exit code 2. Se usa un job con salida
        // propia para no colisionar con el .zip por timestamp ya generado arriba (mismo segundo).
        var exitOptions = new MegnirOptions
        {
            TempDirectory = tempBase,
            OutputDirectory = ws.At("out-exit"),
            SizeWarningMb = 100,
            Apps =
            {
                new AppEntry { Name = "app-a", Paths = { ws.At("src", "app-a", "data"), missing } },
                new AppEntry { Name = "app-b", Paths = { ws.At("src", "app-b", "lib") } },
            },
        };
        var exitCode = await AppRunner.RunAsync(CreateJob(exitOptions), NullLogger.Instance);
        Assert.Equal(AppRunner.ExitPartial, exitCode);
    }

    [Fact]
    public async Task RunAsync_uses_system_temp_when_temp_directory_not_set()
    {
        using var ws = new TempWorkspace();
        ws.CreateFile("a-data", "src", "app-a", "data", "f1.txt");
        var output = ws.At("out");

        var options = new MegnirOptions
        {
            // TempDirectory vacío => Path.GetTempPath().
            OutputDirectory = output,
            SizeWarningMb = 100,
            Apps = { new AppEntry { Name = "app-a", Paths = { ws.At("src", "app-a", "data") } } },
        };

        var result = await CreateJob(options).RunAsync(CancellationToken.None);

        Assert.Equal(BackupOutcome.Success, result.Outcome);
        Assert.True(File.Exists(result.ZipPath));

        // No queda ningún directorio de trabajo residual en el temporal del sistema. El job crea
        // exactamente 'megnir-<32 hex>' (guid "N"); se filtra con ese patrón preciso para no
        // confundirlo con las áreas de trabajo de otros tests ('megnir-*-test-<guid>').
        var leftover = Directory.GetDirectories(Path.GetTempPath(), "megnir-*")
            .Select(Path.GetFileName)
            .Where(name => name is not null &&
                           System.Text.RegularExpressions.Regex.IsMatch(name, "^megnir-[0-9a-f]{32}$"))
            .ToArray();
        Assert.Empty(leftover);
    }
}
