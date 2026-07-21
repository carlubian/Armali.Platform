namespace Megnir.Configuration;

/// <summary>
/// Raíz de la configuración de Megnir (sección "Megnir" de appsettings.json).
/// Refleja el esquema propuesto en el ROADMAP del proyecto.
/// </summary>
public sealed class MegnirOptions
{
    /// <summary>Nombre de la sección de configuración raíz.</summary>
    public const string SectionName = "Megnir";

    /// <summary>
    /// Nombre de la variable de entorno de la que se lee el secreto de conexión de Azure.
    /// El secreto NO se versiona ni se define en appsettings.json (RNF3).
    /// </summary>
    public const string ConnectionStringEnvVar = "MEGNIR_AZURE_CONNECTIONSTRING";

    /// <summary>Aplicaciones a respaldar, cada una con sus rutas de origen.</summary>
    public List<AppEntry> Apps { get; set; } = new();

    /// <summary>Configuración del destino en Azure.</summary>
    public AzureOptions Azure { get; set; } = new();

    /// <summary>Política de retención de copias en la nube.</summary>
    public RetentionOptions Retention { get; set; } = new();

    /// <summary>Tamaño (en MB) a partir del cual se emite un aviso por el log.</summary>
    public int SizeWarningMb { get; set; }
}

/// <summary>Una aplicación a respaldar y las rutas/volúmenes que la componen.</summary>
public sealed class AppEntry
{
    /// <summary>Nombre lógico de la aplicación (se usa como directorio padre en el .zip).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Rutas de origen (carpetas / volúmenes montados) a copiar.</summary>
    public List<string> Paths { get; set; } = new();
}

/// <summary>Configuración del destino de almacenamiento en Azure.</summary>
public sealed class AzureOptions
{
    /// <summary>Contenedor de destino en el Storage Account / Data Lake.</summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Prefijo por host dentro del contenedor. "auto" = usar el hostname del nodo.
    /// </summary>
    public string HostPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Connection string / SAS de Azure. Se puebla EXCLUSIVAMENTE desde la variable de
    /// entorno <see cref="MegnirOptions.ConnectionStringEnvVar"/>, nunca desde el json (RNF3).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>Política de retención "conservar últimas N".</summary>
public sealed class RetentionOptions
{
    /// <summary>Número de copias más recientes a conservar por host.</summary>
    public int KeepLast { get; set; }
}
