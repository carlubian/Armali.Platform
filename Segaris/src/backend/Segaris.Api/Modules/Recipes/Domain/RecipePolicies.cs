using System.Linq.Expressions;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Recipes.Domain;

/// <summary>
/// Visibility rules for Recipes. A recipe is accessible when it is public or was
/// created by the requesting user; private recipes remain creator-only.
/// </summary>
internal static class RecipePolicies
{
    public static Expression<Func<Recipe, bool>> AccessibleTo(UserId userId) =>
        recipe => recipe.Visibility == RecordVisibility.Public || recipe.CreatedBy == userId.Value;

    public static Expression<Func<Recipe, bool>> MutableBy(UserId userId) => AccessibleTo(userId);

    public static bool CanChangeVisibility(Recipe recipe, UserId userId) =>
        recipe.CreatedBy == userId.Value;
}
