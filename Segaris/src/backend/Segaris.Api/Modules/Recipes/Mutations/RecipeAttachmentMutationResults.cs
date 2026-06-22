using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Recipes.Mutations;

internal sealed record RecipeSetPrimaryResult(
    RecipeSetPrimaryOutcome Outcome,
    AttachmentDescriptor? Descriptor);

internal enum RecipeSetPrimaryOutcome
{
    Assigned,
    RecipeNotFound,
    AttachmentNotFound,
    NotImage,
}

internal enum RecipeDeleteAttachmentOutcome
{
    Deleted,
    RecipeNotFound,
    AttachmentNotFound,
}
