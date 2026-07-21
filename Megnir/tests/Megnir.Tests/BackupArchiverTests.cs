using System.IO.Compression;
using Megnir.Backup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Megnir.Tests;

public class BackupArchiverTests
{
    /// <summary>Área de trabajo temporal autolimpiante para las fixtures del test.</summary>
    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; }

        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "megnir-arch-test-" + Guid.NewGuid().ToString("N"));
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

    /// <summary>Logger que acumula las entradas emitidas para poder aseverar sobre ellas.</summary>
    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    /// <summary>Reloj fijo para un naming determinista.</summary>
    private static Func<DateTimeOffset> FixedClock(DateTimeOffset instant) => () => instant;

    /// <summary>Puebla un árbol de trabajo con dos apps y devuelve su ruta raíz.</summary>
    private static string PopulateTwoApps(TempWorkspace ws)
    {
        var work = ws.CreateDir("work");
        ws.CreateFile("a-data", "work", "app-a", "data", "f1.txt");
        ws.CreateFile("a-config", "work", "app-a", "config", "g.txt");
        ws.CreateFile("b-lib", "work", "app-b", "app-b", "h.txt");
        return work;
    }

    [Fact]
    public void CreateArchive_produces_zip_with_expected_per_app_entries()
    {
        using var ws = new TempWorkspace();
        var work = PopulateTwoApps(ws);
        var output = ws.At("out");

        var archiver = new BackupArchiver(NullLogger<BackupArchiver>.Instance);
        var result = archiver.CreateArchive(work, output, sizeWarningMb: 100);

        using var zip = ZipFile.OpenRead(result.ZipPath);
        var names = zip.Entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(
            new[]
            {
                "app-a/config/g.txt",
                "app-a/data/f1.txt",
                "app-b/app-b/h.txt",
            },
            names);

        // Las entradas usan '/' (separador del formato zip), nunca '\'.
        Assert.All(names, n => Assert.DoesNotContain('\\', n));
    }

    [Fact]
    public void CreateArchive_names_zip_by_timestamp_in_output_directory()
    {
        using var ws = new TempWorkspace();
        var work = PopulateTwoApps(ws);
        var output = ws.At("out");

        var instant = new DateTimeOffset(2026, 7, 21, 3, 0, 0, TimeSpan.Zero);
        var archiver = new BackupArchiver(NullLogger<BackupArchiver>.Instance, FixedClock(instant));
        var result = archiver.CreateArchive(work, output, sizeWarningMb: 100);

        Assert.Equal(Path.Combine(output, "megnir-backup-20260721-030000.zip"), result.ZipPath);
        Assert.True(File.Exists(result.ZipPath));
        Assert.Equal(new FileInfo(result.ZipPath).Length, result.SizeBytes);
    }

    [Fact]
    public void CreateArchive_emits_warning_when_size_exceeds_threshold_without_changing_result()
    {
        using var ws = new TempWorkspace();
        var work = PopulateTwoApps(ws);
        var output = ws.At("out");

        var logger = new ListLogger<BackupArchiver>();
        // Umbral 0 => cualquier zip no vacío dispara el aviso.
        var archiver = new BackupArchiver(logger);
        var result = archiver.CreateArchive(work, output, sizeWarningMb: 0);

        // El artefacto se genera con normalidad pese al aviso.
        Assert.True(File.Exists(result.ZipPath));
        Assert.True(result.SizeBytes > 0);

        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("umbral", warning.Message);
    }

    [Fact]
    public void CreateArchive_does_not_warn_when_size_is_below_threshold()
    {
        using var ws = new TempWorkspace();
        var work = PopulateTwoApps(ws);
        var output = ws.At("out");

        var logger = new ListLogger<BackupArchiver>();
        var archiver = new BackupArchiver(logger);
        // Umbral alto: la fixture nunca lo supera.
        archiver.CreateArchive(work, output, sizeWarningMb: 1024);

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public void CreateArchive_leaves_no_intermediate_temp_file_anywhere()
    {
        using var ws = new TempWorkspace();
        var work = PopulateTwoApps(ws);
        var output = ws.At("out");

        var archiver = new BackupArchiver(NullLogger<BackupArchiver>.Instance);
        var result = archiver.CreateArchive(work, output, sizeWarningMb: 100);

        // El destino contiene EXACTAMENTE el .zip final: ningún .zip.tmp ni otro intermedio.
        var outputFiles = Directory.GetFiles(output);
        var only = Assert.Single(outputFiles);
        Assert.Equal(result.ZipPath, Path.GetFullPath(only));
        Assert.EndsWith(".zip", only);
        Assert.DoesNotContain(Directory.GetFiles(output), f => f.EndsWith(".tmp", StringComparison.Ordinal));

        // Tampoco queda un intermedio en el directorio de trabajo.
        Assert.DoesNotContain(
            Directory.GetFiles(work, "*", SearchOption.AllDirectories),
            f => f.EndsWith(".tmp", StringComparison.Ordinal) || f.EndsWith(".zip", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateArchive_creates_output_directory_when_missing()
    {
        using var ws = new TempWorkspace();
        var work = PopulateTwoApps(ws);
        var output = ws.At("nested", "does-not-exist-yet");

        Assert.False(Directory.Exists(output));

        var archiver = new BackupArchiver(NullLogger<BackupArchiver>.Instance);
        var result = archiver.CreateArchive(work, output, sizeWarningMb: 100);

        Assert.True(Directory.Exists(output));
        Assert.True(File.Exists(result.ZipPath));
    }
}
