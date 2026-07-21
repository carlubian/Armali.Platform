using Azure.Storage.Blobs;

namespace Megnir.Configuration;

/// <summary>Valida y resuelve la configuración de Azure necesaria para H2.</summary>
public static class AzureOptionsValidator
{
    /// <summary>
    /// Valida la configuración sin exponer nunca el valor del connection string y resuelve
    /// el prefijo <c>auto</c> a un segmento seguro de blob.
    /// </summary>
    public static ResolvedAzureOptions ValidateAndResolve(AzureOptions options, string? machineName = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateConnectionString(options.ConnectionString);
        ValidateContainerName(options.Container);

        var hostPrefix = string.Equals(options.HostPrefix, "auto", StringComparison.OrdinalIgnoreCase)
            ? NormalizeMachineName(machineName ?? Environment.MachineName)
            : ValidateExplicitHostPrefix(options.HostPrefix);

        return new ResolvedAzureOptions(options.Container, hostPrefix);
    }

    private static void ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Falta la configuración Azure.ConnectionString; defina {MegnirOptions.ConnectionStringEnvVar}.");
        }

        try
        {
            _ = new BlobServiceClient(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("Azure.ConnectionString no tiene un formato válido.", exception);
        }
    }

    private static void ValidateContainerName(string? container)
    {
        if (string.IsNullOrWhiteSpace(container) ||
            container.Length is < 3 or > 63 ||
            !char.IsLetterOrDigit(container[0]) ||
            !char.IsLetterOrDigit(container[^1]) ||
            container.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character == '-')) ||
            container.Contains("--", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Azure.Container debe ser un nombre de contenedor Blob válido.");
        }
    }

    private static string ValidateExplicitHostPrefix(string? hostPrefix)
    {
        if (string.IsNullOrWhiteSpace(hostPrefix) ||
            !string.Equals(hostPrefix, hostPrefix.Trim(), StringComparison.Ordinal) ||
            hostPrefix is "." or ".." ||
            hostPrefix.Contains('/') ||
            hostPrefix.Contains('\\') ||
            hostPrefix.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                "Azure.HostPrefix debe ser un único segmento de blob no ambiguo o el valor 'auto'.");
        }

        return hostPrefix;
    }

    private static string NormalizeMachineName(string machineName)
    {
        var normalized = string.Concat(machineName
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-'))
            .Trim('-');

        if (string.IsNullOrEmpty(normalized))
        {
            throw new InvalidOperationException("No se pudo resolver un prefijo de host seguro desde el nombre de la máquina.");
        }

        return normalized;
    }
}

/// <summary>Configuración Azure ya validada y lista para construir el uploader.</summary>
public sealed record ResolvedAzureOptions(string Container, string HostPrefix);
