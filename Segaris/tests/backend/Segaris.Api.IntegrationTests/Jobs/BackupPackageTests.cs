using System.Formats.Tar;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Segaris.Api.Platform.Backup;
using Segaris.Api.Platform.Jobs;

namespace Segaris.Api.IntegrationTests.Jobs;

/// <summary>
/// Exercises the backup orchestration (dump placement, attachment collection, manifest,
/// packaging, atomic replacement, and staging cleanup) without PostgreSQL or pg_dump by
/// substituting a controllable dump runner. The real pg_dump path is validated separately
/// against PostgreSQL.
/// </summary>
public sealed class BackupPackageTests
{
    [Fact]
    public async Task A_successful_backup_produces_a_validated_package_and_clears_staging()
    {
        using var fixture = new BackupFixture();
        fixture.WriteAttachment("capex", "demo.txt", "attachment payload");

        var result = await fixture.RunBackupAsync();

        Assert.Equal(BackupPaths.PackageFileName, result.ResultReference);
        Assert.True(File.Exists(fixture.PackagePath));

        var entries = fixture.ReadPackageEntryNames();
        Assert.Contains("database.dump", entries);
        Assert.Contains("manifest.json", entries);
        Assert.Contains(entries, name => name.StartsWith("attachments/", StringComparison.Ordinal));

        var manifest = fixture.ReadManifest();
        Assert.NotEqual(JsonValueKind.Null, manifest.GetProperty("createdAt").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(manifest.GetProperty("applicationVersion").GetString()));
        Assert.EndsWith("_CapexDomainPersistence", manifest.GetProperty("schemaVersion").GetString());
        Assert.True(manifest.GetProperty("files").GetArrayLength() >= 2);
        foreach (var file in manifest.GetProperty("files").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(file.GetProperty("sha256").GetString()));
        }

        Assert.Empty(fixture.StagingEntries());
    }

    [Fact]
    public async Task A_failed_backup_preserves_the_previous_package_and_clears_staging()
    {
        using var fixture = new BackupFixture();

        await fixture.RunBackupAsync();
        var preservedBytes = await File.ReadAllBytesAsync(fixture.PackagePath);

        fixture.DumpRunner.ShouldFail = true;
        var failure = await Assert.ThrowsAsync<JobFailureException>(() => fixture.RunBackupAsync());

        Assert.Equal("backup_pg_dump_failed", failure.FailureCode);
        Assert.True(File.Exists(fixture.PackagePath));
        Assert.Equal(preservedBytes, await File.ReadAllBytesAsync(fixture.PackagePath));
        Assert.Empty(fixture.StagingEntries());
    }

    private sealed class BackupFixture : IDisposable
    {
        private readonly WebApplicationFactory<Program> factory;

        public BackupFixture()
        {
            DatabasePath = Path.Combine(Path.GetTempPath(), $"segaris-backup-{Guid.NewGuid():N}.db");
            AttachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-backup-attachments-{Guid.NewGuid():N}");
            BackupsPath = Path.Combine(Path.GetTempPath(), $"segaris-backup-output-{Guid.NewGuid():N}");
            Directory.CreateDirectory(AttachmentsPath);

            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["Segaris:Database:Provider"] = "Sqlite",
                        ["ConnectionStrings:Segaris"] = $"Data Source={DatabasePath}",
                        ["Segaris:Storage:AttachmentsPath"] = AttachmentsPath,
                        ["Segaris:Storage:BackupsPath"] = BackupsPath,
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPostgresDumpRunner>();
                    services.AddSingleton<IPostgresDumpRunner>(DumpRunner);
                });
            });

            // Trigger startup so migrations run and the schema (including __EFMigrationsHistory) exists.
            _ = factory.Services;
        }

        public ConfigurableDumpRunner DumpRunner { get; } = new();

        public string DatabasePath { get; }

        public string AttachmentsPath { get; }

        public string BackupsPath { get; }

        public string PackagePath => Path.Combine(BackupsPath, BackupPaths.PackageFileName);

        public void WriteAttachment(string module, string fileName, string content)
        {
            var directory = Path.Combine(AttachmentsPath, module);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), content);
        }

        public async Task<JobResult> RunBackupAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<BackupJobHandler>();
            var context = new JobExecutionContext(1, "backup", null, (_, _, _, _) => Task.CompletedTask);
            return await handler.ExecuteAsync(context, CancellationToken.None);
        }

        public IReadOnlyList<string> ReadPackageEntryNames()
        {
            var names = new List<string>();
            using var stream = File.OpenRead(PackagePath);
            using var reader = new TarReader(stream);
            while (reader.GetNextEntry() is { } entry)
            {
                names.Add(entry.Name);
            }

            return names;
        }

        public JsonElement ReadManifest()
        {
            using var stream = File.OpenRead(PackagePath);
            using var reader = new TarReader(stream);
            while (reader.GetNextEntry() is { } entry)
            {
                if (entry.Name == "manifest.json")
                {
                    using var entryStream = entry.DataStream!;
                    using var document = JsonDocument.Parse(entryStream);
                    return document.RootElement.Clone();
                }
            }

            throw new InvalidOperationException("The package did not contain a manifest.");
        }

        public IReadOnlyList<string> StagingEntries()
        {
            var staging = Path.Combine(BackupsPath, ".staging");
            return Directory.Exists(staging)
                ? Directory.EnumerateFileSystemEntries(staging).ToArray()
                : [];
        }

        public void Dispose()
        {
            factory.Dispose();
            SqliteConnection.ClearAllPools();
            TryDeleteFile(DatabasePath);
            TryDeleteDirectory(AttachmentsPath);
            TryDeleteDirectory(BackupsPath);
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class ConfigurableDumpRunner : IPostgresDumpRunner
    {
        public bool ShouldFail { get; set; }

        public async Task DumpAsync(string outputPath, CancellationToken cancellationToken)
        {
            if (ShouldFail)
            {
                throw new JobFailureException("backup_pg_dump_failed", "Simulated dump failure.");
            }

            await File.WriteAllTextAsync(outputPath, "simulated dump", cancellationToken);
        }
    }
}
