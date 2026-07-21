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

        Assert.Equal("megnir", options.Azure.Container);
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

    [Fact]
    public void Azure_validation_fails_without_revealing_the_connection_string()
    {
        const string secret = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=c2VjcmV0;EndpointSuffix=core.windows.net";
        var options = new AzureOptions
        {
            ConnectionString = secret,
            Container = "megnir",
            HostPrefix = "host-a"
        };

        options.ConnectionString = " ";
        var exception = Assert.Throws<InvalidOperationException>(() => AzureOptionsValidator.ValidateAndResolve(options));

        Assert.DoesNotContain(secret, exception.ToString(), StringComparison.Ordinal);
        Assert.Contains(MegnirOptions.ConnectionStringEnvVar, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("invalid_container")]
    [InlineData("MeGnir")]
    [InlineData("ab")]
    public void Azure_validation_rejects_invalid_container_names(string container)
    {
        var options = ValidAzureOptions();
        options.Container = container;

        Assert.Throws<InvalidOperationException>(() => AzureOptionsValidator.ValidateAndResolve(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("host/path")]
    [InlineData("host\\path")]
    [InlineData("..")]
    [InlineData(" host")]
    public void Azure_validation_rejects_ambiguous_host_prefixes(string hostPrefix)
    {
        var options = ValidAzureOptions();
        options.HostPrefix = hostPrefix;

        Assert.Throws<InvalidOperationException>(() => AzureOptionsValidator.ValidateAndResolve(options));
    }

    [Fact]
    public void Azure_validation_resolves_auto_host_prefix_to_a_safe_blob_segment()
    {
        var resolved = AzureOptionsValidator.ValidateAndResolve(ValidAzureOptions(), "NODE_01.Example");

        Assert.Equal("megnir", resolved.Container);
        Assert.Equal("node-01-example", resolved.HostPrefix);
    }

    [Fact]
    public void Azure_validation_keeps_a_valid_explicit_host_prefix()
    {
        var options = ValidAzureOptions();
        options.HostPrefix = "Madrid-01";

        var resolved = AzureOptionsValidator.ValidateAndResolve(options);

        Assert.Equal("Madrid-01", resolved.HostPrefix);
    }

    private static AzureOptions ValidAzureOptions() => new()
    {
        ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=c2VjcmV0;EndpointSuffix=core.windows.net",
        Container = "megnir",
        HostPrefix = "auto"
    };
}
