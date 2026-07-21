using Megnir.Configuration;
using Microsoft.Extensions.Configuration;

namespace Megnir.Tests;

public class ConfigurationBindingTests
{
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

    [Fact]
    public void Load_populates_apps_paths_retention_and_size_warning()
    {
        var options = MegnirConfiguration.Load(BuildConfiguration());

        Assert.Equal(2, options.Apps.Count);

        var appA = Assert.Single(options.Apps, a => a.Name == "app-a");
        Assert.Equal(new[] { "/srv/app-a/data", "/srv/app-a/config" }, appA.Paths);

        var appB = Assert.Single(options.Apps, a => a.Name == "app-b");
        Assert.Equal(new[] { "/var/lib/app-b" }, appB.Paths);

        Assert.Equal("megnir-backups", options.Azure.Container);
        Assert.Equal("auto", options.Azure.HostPrefix);
        Assert.Equal(4, options.Retention.KeepLast);
        Assert.Equal(100, options.SizeWarningMb);
    }

    [Fact]
    public void ConnectionString_is_populated_from_environment_variable()
    {
        const string expected =
            "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=c2VjcmV0;EndpointSuffix=core.windows.net";
        var previous = Environment.GetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar, expected);

            var options = MegnirConfiguration.Load(BuildConfiguration());

            Assert.Equal(expected, options.Azure.ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar, previous);
        }
    }

    [Fact]
    public void ConnectionString_is_not_defined_in_appsettings_json()
    {
        var configuration = BuildConfiguration();

        // La clave del secreto no existe en el json versionado.
        Assert.Null(configuration["Megnir:Azure:ConnectionString"]);

        // Y sin la variable de entorno, el binding deja el secreto vacío.
        var previous = Environment.GetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar, null);

            var options = MegnirConfiguration.Load(BuildConfiguration());

            Assert.True(string.IsNullOrEmpty(options.Azure.ConnectionString));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar, previous);
        }

        // El texto crudo del fichero no contiene ningún valor de secreto.
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        Assert.DoesNotContain("AccountKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SharedAccessSignature", json, StringComparison.OrdinalIgnoreCase);
    }
}
