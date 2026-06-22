namespace Segaris.Api.Modules.Recipes.Seeding;

internal static class RecipesSeedExtensions
{
    public static async Task SeedRecipesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RecipesSeeder>().SeedAsync(cancellationToken);
    }
}
