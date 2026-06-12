using Segaris.Api.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Segaris.Api.Platform.Observability;

internal static class LoggingExtensions
{
    private const int SeqBatchPostingLimit = 100;
    private const int SeqQueueSizeLimit = 1_000;
    private const int SeqEventBodyLimitBytes = 64 * 1024;

    public static WebApplicationBuilder AddSegarisLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) =>
        {
            ConfigureMinimumLevels(configuration, builder.Configuration);
            configuration
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Segaris.Api")
                .WriteTo.Console(
                    new RenderedCompactJsonFormatter(),
                    standardErrorFromLevel: LogEventLevel.Warning);

            var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>().Value;
            if (options.Seq.Enabled)
            {
                var minimumLevel = Enum.Parse<LogEventLevel>(options.Seq.MinimumLevel, ignoreCase: true);
                configuration.WriteTo.Seq(
                    options.Seq.ServerUrl!,
                    restrictedToMinimumLevel: minimumLevel,
                    apiKey: string.IsNullOrWhiteSpace(options.Seq.ApiKey) ? null : options.Seq.ApiKey,
                    batchPostingLimit: SeqBatchPostingLimit,
                    period: TimeSpan.FromSeconds(2),
                    queueSizeLimit: SeqQueueSizeLimit,
                    eventBodyLimitBytes: SeqEventBodyLimitBytes);
            }
        });

        return builder;
    }

    private static void ConfigureMinimumLevels(
        LoggerConfiguration loggerConfiguration,
        IConfiguration applicationConfiguration)
    {
        var levels = applicationConfiguration.GetSection("Logging:LogLevel").GetChildren();
        foreach (var level in levels)
        {
            if (!Enum.TryParse<LogEventLevel>(level.Value, ignoreCase: true, out var parsed))
            {
                continue;
            }

            if (string.Equals(level.Key, "Default", StringComparison.OrdinalIgnoreCase))
            {
                loggerConfiguration.MinimumLevel.Is(parsed);
            }
            else
            {
                loggerConfiguration.MinimumLevel.Override(level.Key, parsed);
            }
        }
    }
}
