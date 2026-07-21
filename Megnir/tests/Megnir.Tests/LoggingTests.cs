using Megnir.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Megnir.Tests;

public class LoggingTests
{
    private static IConfiguration BuildConfiguration(string defaultLevel) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = defaultLevel,
            })
            .Build();

    [Fact]
    public void Factory_honours_default_level_from_configuration()
    {
        using var factory = MegnirLogging.CreateFactory(BuildConfiguration("Information"));
        var logger = factory.CreateLogger(MegnirLogging.RootCategory);

        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void Factory_level_is_configurable_via_appsettings_section()
    {
        // Subiendo el nivel a Warning, Information deja de emitirse.
        using var factory = MegnirLogging.CreateFactory(BuildConfiguration("Warning"));
        var logger = factory.CreateLogger(MegnirLogging.RootCategory);

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
    }
}
