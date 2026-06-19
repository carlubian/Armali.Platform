using Segaris.Api.Modules.Assets.Contracts;

namespace Segaris.Api.Modules.Assets.Queries;

/// <summary>
/// Resolves the table thumbnail reference for an asset. The frozen source vocabulary
/// is <c>primary</c> (the marked primary image), <c>image</c> (the first image when
/// none is marked), and <c>placeholder</c> (no image). Wave 2 has no attachments
/// yet, so every asset resolves to the neutral placeholder; Wave 3 enriches this
/// with real primary and first-image resolution once attachments exist.
/// </summary>
internal static class AssetThumbnailResolver
{
    public const string PrimarySource = "primary";
    public const string ImageSource = "image";
    public const string PlaceholderSource = "placeholder";

    public static AssetThumbnailResponse Placeholder() => new(null, null, PlaceholderSource);
}
