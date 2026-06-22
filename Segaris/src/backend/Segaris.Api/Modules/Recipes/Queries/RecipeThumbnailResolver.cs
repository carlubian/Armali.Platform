using System.Globalization;
using Segaris.Api.Modules.Recipes.Contracts;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Recipes.Queries;

/// <summary>Resolves the recipe thumbnail: primary image, first image, or placeholder.</summary>
internal static class RecipeThumbnailResolver
{
    public const string PrimarySource = "primary";
    public const string ImageSource = "image";
    public const string PlaceholderSource = "placeholder";

    public static RecipeThumbnailResponse Resolve(
        int recipeId,
        int? primaryAttachmentId,
        IReadOnlyList<AttachmentDescriptor> descriptors)
    {
        var images = descriptors
            .Where(descriptor => RecipesAttachments.IsImageContentType(descriptor.ContentType))
            .ToArray();

        if (primaryAttachmentId is { } primaryId
            && images.FirstOrDefault(descriptor => descriptor.Id.Value == primaryId) is { } primary)
        {
            return new(
                primary.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(recipeId, primary.Id.Value),
                PrimarySource);
        }

        if (images.Length > 0)
        {
            var first = images[0];
            return new(
                first.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(recipeId, first.Id.Value),
                ImageSource);
        }

        return Placeholder();
    }

    public static RecipeThumbnailResponse Placeholder() => new(null, null, PlaceholderSource);

    private static string DownloadUrl(int recipeId, int attachmentId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"/api/recipes/{recipeId}/attachments/{attachmentId}");
}
