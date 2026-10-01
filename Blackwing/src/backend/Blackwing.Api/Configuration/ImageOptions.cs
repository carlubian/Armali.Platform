namespace Blackwing.Api.Configuration;

/// <summary>
/// Image handling limits and derivative sizes. Every value has a default, so the whole section is
/// optional in configuration.
/// </summary>
internal sealed class ImageOptions
{
    public const string SectionName = "Blackwing:Images";

    /// <summary>Largest accepted upload, in bytes. 100 MB by default.</summary>
    public long MaximumFileSizeBytes { get; set; } = 100L * 1024 * 1024;

    /// <summary>Longest edge of the <c>thumb</c> derivative, in pixels.</summary>
    public int ThumbnailLongestEdge { get; set; } = 400;

    /// <summary>Longest edge of the <c>preview</c> derivative, in pixels.</summary>
    public int PreviewLongestEdge { get; set; } = 1600;

    /// <summary>WebP quality of the <c>thumb</c> derivative, from 1 to 100.</summary>
    public int ThumbnailQuality { get; set; } = 75;

    /// <summary>WebP quality of the <c>preview</c> derivative, from 1 to 100.</summary>
    public int PreviewQuality { get; set; } = 82;
}
