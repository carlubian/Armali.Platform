using System.ComponentModel.DataAnnotations;

namespace Blackwing.Api.Configuration;

public sealed class BlackwingOptions
{
    public const string SectionName = "Blackwing";
    [Required] public StorageOptions Storage { get; init; } = new();
    [Required] public IngestionOptions Ingestion { get; init; } = new();
}

public sealed class StorageOptions
{
    /// <summary>Durable, content-addressed image tree (originals + derivatives).</summary>
    [Required] public string ImagesPath { get; init; } = string.Empty;

    /// <summary>
    /// Transient holding area for uploaded bytes awaiting processing. Kept on the
    /// same persistent volume as the images so a crash mid-upload does not lose
    /// staged bytes — the worker recovers them on restart.
    /// </summary>
    [Required] public string StagingPath { get; init; } = string.Empty;
}

public sealed class IngestionOptions
{
    /// <summary>Maximum accepted size of a single uploaded file (default 100 MB).</summary>
    [Range(1, long.MaxValue)] public long MaxFileBytes { get; init; } = 104_857_600;

    /// <summary>Longest edge of the medium preview derivative, in pixels.</summary>
    [Range(1, 20000)] public int PreviewMaxEdge { get; init; } = 1600;

    /// <summary>Longest edge of the grid thumbnail derivative, in pixels.</summary>
    [Range(1, 4000)] public int ThumbnailMaxEdge { get; init; } = 400;

    /// <summary>WebP encoder quality for generated derivatives (1–100).</summary>
    [Range(1, 100)] public int WebpQuality { get; init; } = 80;

    /// <summary>How often the worker polls for pending jobs when idle, in seconds.</summary>
    [Range(1, 600)] public int PollSeconds { get; init; } = 5;

    /// <summary>Upper bound on the memory ImageMagick may use to decode one image.</summary>
    [Range(1, long.MaxValue)] public long ProcessingMemoryLimitBytes { get; init; } = 1_073_741_824;
}
