using Megnir.Configuration;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var options = MegnirConfiguration.Load(configuration);

Console.WriteLine(
    $"Megnir backup service - configuración cargada: {options.Apps.Count} app(s), " +
    $"container='{options.Azure.Container}', Retention.KeepLast={options.Retention.KeepLast}, " +
    $"SizeWarningMb={options.SizeWarningMb}.");

// Nunca se imprime el secreto; solo si está presente o no.
Console.WriteLine(
    string.IsNullOrWhiteSpace(options.Azure.ConnectionString)
        ? $"Aviso: sin connection string de Azure (defina la variable de entorno {MegnirOptions.ConnectionStringEnvVar})."
        : "Connection string de Azure cargada desde variable de entorno.");

return 0;
