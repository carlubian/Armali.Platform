using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Blackwing.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the API in the <c>Testing</c> environment against the container PostgreSQL instance and a
/// throwaway image directory, so the readiness probe exercises the real dependencies.
/// </summary>
public sealed class BlackwingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public BlackwingApiFactory(string connectionString)
    {
        _connectionString = connectionString;
        ImagesPath = Path.Combine(Path.GetTempPath(), $"blackwing-images-{Guid.NewGuid():N}");
    }

    public string ImagesPath { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Blackwing"] = _connectionString,
                ["Blackwing:Storage:ImagesPath"] = ImagesPath,
                ["Blackwing:Storage:DataProtectionKeysPath"] = string.Empty,
                ["Blackwing:Observability:Seq:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Warning",
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            if (Directory.Exists(ImagesPath))
            {
                Directory.Delete(ImagesPath, recursive: true);
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
