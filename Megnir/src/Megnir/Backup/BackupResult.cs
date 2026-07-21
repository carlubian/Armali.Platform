namespace Megnir.Backup;

/// <summary>
/// Resultado de una ejecución del <see cref="IBackupJob"/>. Modela el desenlace global
/// (<see cref="BackupOutcome"/>), los errores parciales acumulados (best-effort por
/// ruta/app), la ruta del <c>.zip</c> generado y su tamaño en bytes.
/// </summary>
/// <remarks>
/// Lo ensambla el <see cref="LocalBackupJob"/> (Fase 3 de H1) a partir de los errores parciales que
/// devuelve el copiador y del artefacto que produce el empaquetador: <see cref="BackupOutcome.Success"/>
/// si no hubo errores, <see cref="BackupOutcome.Partial"/> si el <c>.zip</c> se generó pero alguna
/// ruta/fichero falló. <see cref="PartialErrors"/>, <see cref="ZipPath"/> y <see cref="SizeBytes"/> se
/// rellenan con el desenlace real.
/// </remarks>
public sealed class BackupResult
{
    /// <summary>Desenlace global de la ejecución.</summary>
    public BackupOutcome Outcome { get; set; } = BackupOutcome.Success;

    /// <summary>
    /// Errores parciales registrados durante la copia (una ruta inexistente, un fichero
    /// bloqueado, etc.). Si tiene elementos y el zip se generó, el desenlace es
    /// <see cref="BackupOutcome.Partial"/>.
    /// </summary>
    public IList<BackupError> PartialErrors { get; } = new List<BackupError>();

    /// <summary>
    /// Ruta absoluta del <c>.zip</c> generado. Vacía cuando aún no hay artefacto (Fase 0
    /// o fallo total).
    /// </summary>
    public string ZipPath { get; set; } = string.Empty;

    /// <summary>Tamaño en bytes del <c>.zip</c> generado (0 si no hay artefacto).</summary>
    public long SizeBytes { get; set; }
}

/// <summary>Desenlace global de un backup.</summary>
public enum BackupOutcome
{
    /// <summary>Éxito total: todas las rutas copiadas, <c>.zip</c> generado.</summary>
    Success,

    /// <summary>Parcial: <c>.zip</c> generado pero alguna ruta/fichero falló.</summary>
    Partial,

    /// <summary>Fallo: no hay <c>.zip</c> fiable.</summary>
    Failed
}

/// <summary>
/// Un error parcial asociado a una app y (opcionalmente) a una ruta de origen concreta.
/// </summary>
public sealed class BackupError
{
    public BackupError(string app, string path, string message)
    {
        App = app ?? string.Empty;
        Path = path ?? string.Empty;
        Message = message ?? string.Empty;
    }

    /// <summary>Nombre de la app afectada.</summary>
    public string App { get; }

    /// <summary>Ruta de origen afectada (puede ir vacía si el error es de la app entera).</summary>
    public string Path { get; }

    /// <summary>Descripción del fallo.</summary>
    public string Message { get; }
}
