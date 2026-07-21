using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Megnir.Logging;

/// <summary>
/// Configuración centralizada del logging de Megnir: proveedor de consola simple con
/// una línea por entrada, sin colores y con timestamp legible, apto para journald/stdout.
/// </summary>
/// <remarks>
/// En la Fase 3 se introducirá el Generic Host (<c>Host.CreateApplicationBuilder</c>), que
/// integra logging+config+DI. El host lee automáticamente la sección "Logging" del
/// appsettings; para absorber esta fase con el mínimo rework bastará con reutilizar
/// <see cref="AddMegnirConsole"/> sobre <c>builder.Logging</c> (previo
/// <c>ClearProviders()</c>) en lugar de <see cref="CreateFactory"/>.
/// </remarks>
public static class MegnirLogging
{
    /// <summary>Categoría raíz de los logs de arranque/cierre del proceso.</summary>
    public const string RootCategory = "Megnir";

    /// <summary>
    /// Registra el proveedor de consola de Megnir: una sola línea por entrada, sin
    /// colores y con timestamp UTC legible. Evita el output multilínea por defecto.
    /// </summary>
    public static ILoggingBuilder AddMegnirConsole(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.ColorBehavior = LoggerColorBehavior.Disabled;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.UseUtcTimestamp = true;
        });

        return builder;
    }

    /// <summary>
    /// Crea un <see cref="ILoggerFactory"/> autónomo (sin Generic Host) con el nivel de
    /// log tomado de la sección "Logging" de <paramref name="configuration"/> y el
    /// proveedor de consola de Megnir.
    /// </summary>
    public static ILoggerFactory CreateFactory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddMegnirConsole();
        });
    }
}
