namespace Blackwing.Shared.Images;

/// <summary>The three stored renditions of one image.</summary>
public enum ImageVariant
{
    /// <summary>The uploaded bytes, never transformed.</summary>
    Original = 0,

    /// <summary>Small WebP rendition for grids.</summary>
    Thumbnail = 1,

    /// <summary>Large WebP rendition for the viewer.</summary>
    Preview = 2,
}
