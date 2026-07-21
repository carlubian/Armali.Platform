using Megnir.Backup;
using Megnir.Configuration;
using Megnir.Hosting;
using Megnir.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Generic Host one-shot: se construye el host para obtener configuración + logging + DI,
// pero NO se arranca como servicio de larga vida (nada de host.Run() ni IHostedService en
// bucle). Se resuelve el IBackupJob, se ejecuta una vez y el proceso termina con el exit
// code que devuelve AppRunner (0 = éxito, ≠ 0 = fallo; RNF4).
var builder = Host.CreateApplicationBuilder(args);

// Configuración: appsettings.json (junto al binario) + variables de entorno. El secreto de
// Azure NO está en el json; se inyecta por env var MEGNIR_AZURE_CONNECTIONSTRING (RNF3).
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables();

// Logging: se reutiliza el proveedor de consola de Megnir (una línea, sin colores, con
// timestamp) sobre builder.Logging, sin duplicar el formato. Se limpian los proveedores
// por defecto del host para no emitir además el formato multilínea estándar.
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddMegnirConsole();

// Opciones: binding de la sección "Megnir" a MegnirOptions, disponible vía IOptions<>.
builder.Services.Configure<MegnirOptions>(builder.Configuration.GetSection(MegnirOptions.SectionName));

// El secreto de Azure se resuelve desde la env var y se aplica sobre las opciones ya
// bindeadas (nunca desde el json). Se hace en un post-configure para que IOptions lo vea.
builder.Services.PostConfigure<MegnirOptions>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar);
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.Azure.ConnectionString = connectionString;
    }
});

// Job de backup: implementación placeholder para H0 (NoOpBackupJob). En H1–H3 se sustituye
// el registro por la implementación real sin tocar el flujo one-shot.
builder.Services.AddSingleton<IBackupJob, NoOpBackupJob>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(MegnirLogging.RootCategory);
logger.LogInformation("Megnir backup service iniciando (ejecución one-shot).");

var options = host.Services.GetRequiredService<IOptions<MegnirOptions>>().Value;
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

// Ejecución one-shot del job + manejo global de errores → exit code.
var job = host.Services.GetRequiredService<IBackupJob>();
var exitCode = await AppRunner.RunAsync(job, logger).ConfigureAwait(false);

logger.LogInformation("Megnir backup service finalizado (exit code {ExitCode}).", exitCode);
return exitCode;
