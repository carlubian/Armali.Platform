using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Segaris.Api.Persistence;
using Segaris.Api.Platform.Persistence;
using Segaris.Persistence;
using Testcontainers.PostgreSql;

namespace Segaris.Postgres.IntegrationTests;

public sealed class PostgresPersistenceTests : IAsyncLifetime
{
    private PostgreSqlContainer? postgres;

    public async Task InitializeAsync()
    {
        try
        {
            postgres = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("segaris_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
            await postgres.StartAsync();
        }
        catch (DockerUnavailableException) when (!IsContinuousIntegration())
        {
            postgres = null;
        }
    }

    public Task DisposeAsync()
    {
        return postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }

    [Fact]
    public async Task Postgres_supports_foundation_mappings_constraints_and_queries()
    {
        if (postgres is null)
        {
            return;
        }

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var createdAt = new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero);

        database.Set<PersistenceCompatibilityRecord>().Add(new PersistenceCompatibilityRecord
        {
            Name = "Foundation fixture",
            CivilDate = new DateOnly(2026, 6, 12),
            Amount = 1234.56m,
            CurrencyCode = "EUR",
            CreatedAt = createdAt,
        });
        await database.SaveChangesAsync();

        var result = await database.Set<PersistenceCompatibilityRecord>()
            .AsNoTracking()
            .SingleAsync(record => record.Name.Contains("Foundation"));

        Assert.True(result.Id > 0);
        Assert.Equal(new DateOnly(2026, 6, 12), result.CivilDate);
        Assert.Equal(1234.56m, result.Amount);
        Assert.Equal("EUR", result.CurrencyCode);
        Assert.Equal(createdAt, result.CreatedAt);

        database.Set<PersistenceCompatibilityRecord>().Add(new PersistenceCompatibilityRecord
        {
            Name = result.Name,
            CivilDate = result.CivilDate,
            Amount = result.Amount,
            CurrencyCode = result.CurrencyCode,
            CreatedAt = result.CreatedAt,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());
    }

    [Fact]
    public async Task Postgres_supports_the_identity_user_lifecycle()
    {
        if (postgres is null)
        {
            return;
        }

        const string adminUserName = "pg-founder";
        const string adminPassword = "PgFounderPass123!";

        await using var factory = CreateFactory(
            new("Segaris:Identity:Bootstrap:UserName", adminUserName),
            new("Segaris:Identity:Bootstrap:Password", adminPassword));
        using var admin = factory.CreateClient();

        using var login = await admin.PostAsJsonAsync(
            "/api/session",
            new { userName = adminUserName, password = adminPassword },
            CancellationToken.None);
        login.EnsureSuccessStatusCode();

        using var created = await PostWithCsrfAsync(
            admin,
            "/api/admin/users",
            new { userName = "pg-member", password = "PgMemberPass123!", role = "User" });
        using var duplicate = await PostWithCsrfAsync(
            admin,
            "/api/admin/users",
            new { userName = "pg-member", password = "PgMemberPass123!", role = "User" });

        var list = await admin.GetFromJsonAsync<JsonElement>("/api/admin/users", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(2, list.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Postgres_supports_attachment_metadata_lifecycle()
    {
        if (postgres is null)
        {
            return;
        }

        const string adminUserName = "pg-attachment-admin";
        const string adminPassword = "PgAttachmentPass123!";
        var attachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-pg-attachments-{Guid.NewGuid():N}");
        try
        {
            await using var factory = CreateFactory(
                new("Segaris:Storage:AttachmentsPath", attachmentsPath),
                new("Segaris:Identity:Bootstrap:UserName", adminUserName),
                new("Segaris:Identity:Bootstrap:Password", adminPassword));
            using var client = factory.CreateClient();
            using var login = await client.PostAsJsonAsync(
                "/api/session",
                new { userName = adminUserName, password = adminPassword },
                CancellationToken.None);
            login.EnsureSuccessStatusCode();

            var token = await client.GetFromJsonAsync<JsonElement>(
                "/api/session/antiforgery",
                CancellationToken.None);
            using var multipart = new MultipartFormDataContent();
            var file = new ByteArrayContent("postgres attachment"u8.ToArray());
            file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipart.Add(file, "file", "postgres.txt");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/attachments")
            {
                Content = multipart,
            };
            request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("csrfToken").GetString());
            using var created = await client.SendAsync(request, CancellationToken.None);
            var descriptor = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
            var id = descriptor.GetProperty("id").GetProperty("value").GetInt32();

            var metadata = await client.GetFromJsonAsync<JsonElement>(
                $"/api/platform/attachments/{id}/metadata",
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal("postgres.txt", metadata.GetProperty("fileName").GetString());
        }
        finally
        {
            try
            {
                Directory.Delete(attachmentsPath, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    [Fact]
    public async Task Postgres_upgrades_from_the_previous_identity_schema()
    {
        if (postgres is null)
        {
            return;
        }

        var schema = $"attachment_upgrade_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{schema}\"";
            await command.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            SearchPath = schema,
        }.ConnectionString;
        await using var database = new SegarisDesignTimeDbContextFactory().CreateDbContext(
        [
            "--provider",
            "Postgres",
            "--connection",
            connectionString,
        ]);
        var identityMigration = database.Database.GetMigrations()
            .Single(migration => migration.EndsWith("_IdentityFoundation", StringComparison.Ordinal));
        var migrator = database.GetService<IMigrator>();

        await migrator.MigrateAsync(identityMigration);
        await migrator.MigrateAsync();

        var applied = await database.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, migration => migration.EndsWith("_AttachmentStorage"));
        await database.Database.OpenConnectionAsync();
        await using var countCommand = database.Database.GetDbConnection().CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM platform_attachments";
        Assert.Equal(0L, (long)(await countCommand.ExecuteScalarAsync())!);
    }

    private WebApplicationFactory<Program> CreateFactory(
        params KeyValuePair<string, string?>[] additionalSettings)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    var settings = new List<KeyValuePair<string, string?>>
                    {
                        new("Segaris:Database:Provider", "Postgres"),
                        new("ConnectionStrings:Segaris", postgres!.GetConnectionString()),
                    };
                    settings.AddRange(additionalSettings);
                    configuration.AddInMemoryCollection(settings);
                });
            });
    }

    private static async Task<HttpResponseMessage> PostWithCsrfAsync(
        HttpClient client,
        string url,
        object body)
    {
        var token = await client.GetFromJsonAsync<JsonElement>(
            "/api/session/antiforgery",
            CancellationToken.None);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("csrfToken").GetString());
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static bool IsContinuousIntegration()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("CI"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
