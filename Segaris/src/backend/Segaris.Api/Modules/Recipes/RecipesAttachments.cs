using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Recipes;

/// <summary>Attachment owner identifiers published by the Recipes module.</summary>
internal static class RecipesAttachments
{
    public const string Module = "Recipes";
    public const string RecipeEntityType = "Recipe";

    public static AttachmentOwner RecipeOwner(int recipeId) =>
        new(Module, RecipeEntityType, recipeId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Only image attachments can drive the gallery thumbnail or be marked as the
    /// primary image. The attachment subsystem stores canonical content types
    /// (<c>image/jpeg</c>, <c>image/png</c>, <c>image/webp</c>), so a prefix match is
    /// sufficient and stable.
    /// </summary>
    public static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
