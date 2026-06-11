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
            new("ConnectionStrings:Segaris", "Data Source=test.db"),
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
            new("ConnectionStrings:Segaris", "Data Source=test.db"),
        ]);

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task Valid_configuration_starts_a_healthy_api()
    {
        using var factory = CreateFactory(
        [
            new("Segaris:Database:Provider", "Sqlite"),
            new("ConnectionStrings:Segaris", "Data Source=test.db"),
        ]);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", CancellationToken.None);

        response.EnsureSuccessStatusCode();
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
