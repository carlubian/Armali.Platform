using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Segaris.Api.IntegrationTests;

public sealed class ConfigurationTests
{
    [Fact]
    public void Startup_rejects_missing_database_configuration()
    {
        using var factory = CreateFactory([]);

        Assert.ThrowsAny<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public void Startup_rejects_an_unknown_database_provider()
    {
        using var factory = CreateFactory(
        [
            new("Segaris:Database:Provider", "Unknown"),
            new("ConnectionStrings:Segaris", "Data Source=:memory:"),
        ]);

        Assert.ThrowsAny<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public void Startup_rejects_unknown_database_properties()
    {
        using var factory = CreateFactory(
        [
            new("Segaris:Database:Provider", "Sqlite"),
            new("Segaris:Database:Unexpected", "value"),
            new("ConnectionStrings:Segaris", "Data Source=:memory:"),
        ]);

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task Valid_configuration_starts_a_healthy_api()
    {
        using var factory = CreateFactory(
        [
            new("Segaris:Database:Provider", "Sqlite"),
            new("ConnectionStrings:Segaris", "Data Source=:memory:"),
        ]);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public void Startup_rejects_unknown_storage_properties()
    {
        using var factory = CreateFactory(
        [
            new("Segaris:Database:Provider", "Sqlite"),
            new("ConnectionStrings:Segaris", "Data Source=:memory:"),
            new("Segaris:Storage:Unexpected", "value"),
        ]);

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }

    [Fact]
    public void Startup_stops_when_database_migration_fails()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing", "test.db");
        using var factory = CreateFactory(
        [
            new("Segaris:Database:Provider", "Sqlite"),
            new("ConnectionStrings:Segaris", $"Data Source={invalidPath}"),
        ]);

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IEnumerable<KeyValuePair<string, string?>> settings)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(settings);
                });
            });
    }
}
