using Megnir.Configuration;
using Megnir.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// Logging a stdout: una línea por entrada, sin colores, con timestamp; nivel desde appsettings.
// En la Fase 3 el Generic Host (Host.CreateApplicationBuilder) absorberá esta LoggerFactory
// reutilizando MegnirLogging.AddMegnirConsole sobre builder.Logging.
using var loggerFactory = MegnirLogging.CreateFactory(configuration);
var logger = loggerFactory.CreateLogger(MegnirLogging.RootCategory);

logger.LogInformation("Megnir backup service iniciando (ejecución one-shot).");

var options = MegnirConfiguration.Load(configuration);

logger.LogInformation(
    "Configuración cargada: {AppCount} app(s), container={Container}, Retention.KeepLast={KeepLast}, SizeWarningMb={SizeWarningMb}.",
    options.Apps.Count, options.Azure.Container, options.Retention.KeepLast, options.SizeWarningMb);

// Nunca se loguea el secreto; solo si está presente o no.
if (string.IsNullOrWhiteSpace(options.Azure.ConnectionString))
{
    logger.LogWarning(
        "Sin connection string de Azure (defina la variable de entorno {EnvVar}).",
        MegnirOptions.ConnectionStringEnvVar);
}
else
{
    logger.LogInformation("Connection string de Azure cargada desde variable de entorno.");
}

logger.LogInformation("Megnir backup service finalizado con éxito (exit code 0).");
return 0;
