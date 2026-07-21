using Microsoft.Extensions.Configuration;

namespace Megnir.Configuration;

/// <summary>
/// Carga y binding de <see cref="MegnirOptions"/> a partir de una configuración.
/// </summary>
public static class MegnirConfiguration
{
    /// <summary>
    /// Bindea la sección "Megnir" a un <see cref="MegnirOptions"/> y puebla el secreto
    /// de Azure desde la variable de entorno <see cref="MegnirOptions.ConnectionStringEnvVar"/>.
    /// El secreto nunca proviene del appsettings.json (RNF3).
    /// </summary>
    public static MegnirOptions Load(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(MegnirOptions.SectionName).Get<MegnirOptions>()
                      ?? new MegnirOptions();

        var connectionString = Environment.GetEnvironmentVariable(MegnirOptions.ConnectionStringEnvVar);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            options.Azure.ConnectionString = connectionString;
        }

        return options;
    }
}
