using Blackwing.Shared.Images;

namespace Blackwing.Api.Platform.Images;

/// <summary>
/// What decoding an image produces: its logical size, and its encoded derivatives.
/// </summary>
/// <param name="Width">Width after the EXIF orientation has been applied.</param>
/// <param name="Height">Height after the EXIF orientation has been applied.</param>
/// <param name="Derivatives">The encoded WebP renditions, keyed by variant.</param>
internal sealed record ProcessedImage(
    int Width,
    int Height,
    IReadOnlyDictionary<ImageVariant, byte[]> Derivatives);

/// <summary>
/// Decodes an uploaded image and produces its derivatives. The one seam through which the rest
/// of the application touches an imaging library.
/// </summary>
internal interface IImageProcessor
{
    /// <exception cref="Blackwing.Api.Platform.Api.ApiProblemException">
    /// The content is not a decodable image.
    /// </exception>
    ProcessedImage Process(Stream content);
}
