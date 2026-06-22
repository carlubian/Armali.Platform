namespace Segaris.Api.Modules.Recipes.Contracts;

internal sealed record RecipeCategoryResponse(int Id, string Name, int SortOrder);

internal sealed record RecipeAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt,
    bool IsPrimary);

internal sealed record RecipeThumbnailResponse(
    string? AttachmentId,
    string? Url,
    string Source);

/// <summary>
/// A projected ingredient line. <see cref="ItemId"/> is the live Inventory item
/// reference and <see cref="ItemName"/> is its resolved display name, or
/// <see langword="null"/> when the item is not resolvable for the viewer, in which
/// case the line still carries its free-text <see cref="Name"/>.
/// </summary>
internal sealed record RecipeIngredientResponse(
    int Id,
    string Name,
    string? Quantity,
    int? ItemId,
    string? ItemName,
    int Position);

internal sealed record RecipeStepResponse(int Id, string Instruction, int Position);

internal sealed record RecipeSummaryResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string? Difficulty,
    string Visibility,
    RecipeThumbnailResponse Thumbnail,
    int CreatorId,
    string CreatorName);

internal sealed record RecipeResponse(
    int Id,
    string Name,
    int CategoryId,
    string CategoryName,
    string? Difficulty,
    int? Servings,
    int? PreparationMinutes,
    int? CookMinutes,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    IReadOnlyList<RecipeStepResponse> Steps,
    string? Notes,
    string Visibility,
    RecipeThumbnailResponse Thumbnail,
    IReadOnlyList<RecipeAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// One recipe reference inside a menu slot, with the resolved recipe name and
/// gallery thumbnail or a neutral placeholder when the recipe is not resolvable for
/// the viewer.
/// </summary>
internal sealed record WeeklyMenuSlotRecipeResponse(
    int RecipeId,
    string? RecipeName,
    RecipeThumbnailResponse Thumbnail);

internal sealed record WeeklyMenuSlotResponse(
    string Day,
    string Slot,
    IReadOnlyList<WeeklyMenuSlotRecipeResponse> Recipes);

internal sealed record WeeklyMenuSummaryResponse(
    int Id,
    DateOnly Week,
    string? Name,
    string Visibility,
    int CreatorId,
    string CreatorName);

internal sealed record WeeklyMenuResponse(
    int Id,
    DateOnly Week,
    string? Name,
    string Visibility,
    IReadOnlyList<WeeklyMenuSlotResponse> Slots,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
