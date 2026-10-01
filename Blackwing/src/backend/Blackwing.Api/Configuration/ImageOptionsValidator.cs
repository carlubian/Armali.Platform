using Microsoft.Extensions.Options;

namespace Blackwing.Api.Configuration;

internal sealed class ImageOptionsValidator : IValidateOptions<ImageOptions>
{
    public const long MinimumFileSizeBytes = 1024L * 1024;
    public const long MaximumAllowedFileSizeBytes = 1024L * 1024 * 1024;
    public const int MinimumEdge = 16;
    public const int MaximumEdge = 8192;

    public ValidateOptionsResult Validate(string? name, ImageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaximumFileSizeBytes is < MinimumFileSizeBytes or > MaximumAllowedFileSizeBytes)
        {
            return ValidateOptionsResult.Fail(
                $"{ImageOptions.SectionName}:MaximumFileSizeBytes must be between "
                + $"{MinimumFileSizeBytes} and {MaximumAllowedFileSizeBytes}.");
        }

        if (options.ThumbnailLongestEdge is < MinimumEdge or > MaximumEdge)
        {
            return ValidateOptionsResult.Fail(
                $"{ImageOptions.SectionName}:ThumbnailLongestEdge must be between "
                + $"{MinimumEdge} and {MaximumEdge}.");
        }

        if (options.PreviewLongestEdge is < MinimumEdge or > MaximumEdge)
        {
            return ValidateOptionsResult.Fail(
                $"{ImageOptions.SectionName}:PreviewLongestEdge must be between "
                + $"{MinimumEdge} and {MaximumEdge}.");
        }

        if (options.PreviewLongestEdge <= options.ThumbnailLongestEdge)
        {
            return ValidateOptionsResult.Fail(
                $"{ImageOptions.SectionName}:PreviewLongestEdge must be greater than ThumbnailLongestEdge.");
        }

        if (options.ThumbnailQuality is < 1 or > 100)
        {
            return ValidateOptionsResult.Fail(
                $"{ImageOptions.SectionName}:ThumbnailQuality must be between 1 and 100.");
        }

        if (options.PreviewQuality is < 1 or > 100)
        {
            return ValidateOptionsResult.Fail(
                $"{ImageOptions.SectionName}:PreviewQuality must be between 1 and 100.");
        }

        return ValidateOptionsResult.Success;
    }
}
