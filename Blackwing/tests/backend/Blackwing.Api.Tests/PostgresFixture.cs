using Blackwing.Persistence;
using Blackwing.Persistence.Identity;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Blackwing.Api.Tests;

/// <summary>
/// Shared PostgreSQL container + application factory for the persistence and
/// storage integration tests. When Docker is not available locally the fixture
/// stays disabled and its tests skip; continuous integration always has Docker,
/// so the coverage runs there.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    private string imagesRoot = string.Empty;

    public bool Available => Factory is not null;
    public WebApplicationFactory<Program>? Factory { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            container = new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase("blackwing_test")
                .WithUsername("blackwing")
                .WithPassword("blackwing")
                .Build();
            await container.StartAsync();
        }
        catch (DockerUnavailableException) when (!IsContinuousIntegration())
        {
            container = null;
            return;
        }

        imagesRoot = Path.Combine(Path.GetTempPath(), "blackwing-it", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(imagesRoot);
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Blackwing"] = container.GetConnectionString(),
                    ["Blackwing:Storage:ImagesPath"] = imagesRoot,
                });
            });
        });

        // Force host startup so migrations and identity seeding run once.
        using var _ = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (container is not null) await container.DisposeAsync();
        if (Directory.Exists(imagesRoot))
        {
            try { Directory.Delete(imagesRoot, recursive: true); } catch (IOException) { }
        }
    }

    /// <summary>Creates a distinct Blackwing user and returns its id, so tests never share owners.</summary>
    public static async Task<Guid> CreateUserAsync(BlackwingDbContext database, string userName)
    {
        var id = Guid.NewGuid();
        database.Users.Add(new BlackwingUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
        });
        await database.SaveChangesAsync();
        return id;
    }

    private static bool IsContinuousIntegration() =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
