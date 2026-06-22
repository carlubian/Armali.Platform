using Segaris.Api.Modules.Recipes.Contracts;

namespace Segaris.Api.IntegrationTests.Recipes;

internal sealed class RecipeRequestBuilder
{
    private string? name = "Tortilla";
    private int categoryId;
    private string? difficulty;
    private int? servings;
    private int? preparationMinutes;
    private int? cookMinutes;
    private IReadOnlyList<RecipeIngredientRequest> ingredients = [];
    private IReadOnlyList<RecipeStepRequest> steps = [];
    private string? notes;
    private string? visibility = "Public";

    public static RecipeRequestBuilder Default() => new();

    public RecipeRequestBuilder WithName(string? value) { name = value; return this; }
    public RecipeRequestBuilder WithCategory(int value) { categoryId = value; return this; }
    public RecipeRequestBuilder WithDifficulty(string? value) { difficulty = value; return this; }
    public RecipeRequestBuilder WithServings(int? value) { servings = value; return this; }
    public RecipeRequestBuilder WithPreparationMinutes(int? value) { preparationMinutes = value; return this; }
    public RecipeRequestBuilder WithCookMinutes(int? value) { cookMinutes = value; return this; }
    public RecipeRequestBuilder WithIngredients(params RecipeIngredientRequest[] values) { ingredients = values; return this; }
    public RecipeRequestBuilder WithSteps(params RecipeStepRequest[] values) { steps = values; return this; }
    public RecipeRequestBuilder WithNotes(string? value) { notes = value; return this; }
    public RecipeRequestBuilder WithVisibility(string? value) { visibility = value; return this; }

    public CreateRecipeRequest BuildCreate() => new(
        name,
        categoryId,
        difficulty,
        servings,
        preparationMinutes,
        cookMinutes,
        ingredients,
        steps,
        notes,
        visibility);

    public UpdateRecipeRequest BuildUpdate() => new(
        name,
        categoryId,
        difficulty,
        servings,
        preparationMinutes,
        cookMinutes,
        ingredients,
        steps,
        notes,
        visibility);
}
