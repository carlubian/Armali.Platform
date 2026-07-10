namespace Blackwing.Shared.Storage;

/// <summary>The three stored representations of an uploaded image.</summary>
public enum ImageDerivative
{
    /// <summary>The untouched original bytes, served only on explicit demand.</summary>
    Original,
    /// <summary>The medium, web-friendly preview shown in the detail view.</summary>
    Preview,
    /// <summary>The small, web-friendly thumbnail shown in gallery grids.</summary>
    Thumbnail,
}
