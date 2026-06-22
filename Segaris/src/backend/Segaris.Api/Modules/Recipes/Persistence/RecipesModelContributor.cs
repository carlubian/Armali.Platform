using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Recipes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Recipes.Persistence;

internal sealed class RecipesModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureRecipeCategory(modelBuilder);
        ConfigureRecipe(modelBuilder);
        ConfigureRecipeIngredient(modelBuilder);
        ConfigureRecipeStep(modelBuilder);
        ConfigureWeeklyMenu(modelBuilder);
        ConfigureWeeklyMenuSlotRecipe(modelBuilder);
    }

    private static void ConfigureRecipeCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecipeCategory>(builder =>
        {
            builder.ToTable("recipe_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name)
                .HasMaxLength(RecipesDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName)
                .HasMaxLength(RecipesDefaults.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });
    }

    private static void ConfigureRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(builder =>
        {
            builder.ToTable("recipes");
            builder.HasKey(recipe => recipe.Id);
            builder.Property(recipe => recipe.Id).ValueGeneratedOnAdd();
            builder.Property(recipe => recipe.Name)
                .HasMaxLength(RecipesDefaults.NameMaximumLength).IsRequired();
            builder.Property(recipe => recipe.Difficulty)
                .HasConversion<string>().HasMaxLength(10);
            builder.Property(recipe => recipe.Notes)
                .HasMaxLength(RecipesDefaults.NotesMaximumLength);
            builder.Property(recipe => recipe.Visibility)
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(recipe => recipe.CreatedAt).IsRequired();
            builder.Property(recipe => recipe.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_recipes_visibility",
                    "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint(
                    "CK_recipes_difficulty",
                    "\"Difficulty\" IS NULL OR \"Difficulty\" IN ('Easy', 'Medium', 'Hard')");
                table.HasCheckConstraint(
                    "CK_recipes_servings",
                    "\"Servings\" IS NULL OR \"Servings\" > 0");
                table.HasCheckConstraint(
                    "CK_recipes_preparation_minutes",
                    "\"PreparationMinutes\" IS NULL OR \"PreparationMinutes\" > 0");
                table.HasCheckConstraint(
                    "CK_recipes_cook_minutes",
                    "\"CookMinutes\" IS NULL OR \"CookMinutes\" > 0");
            });
            builder.HasMany(recipe => recipe.Ingredients)
                .WithOne()
                .HasForeignKey(ingredient => ingredient.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(recipe => recipe.Ingredients)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(recipe => recipe.Steps)
                .WithOne()
                .HasForeignKey(step => step.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(recipe => recipe.Steps)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<RecipeCategory>()
                .WithMany()
                .HasForeignKey(recipe => recipe.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(recipe => recipe.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(recipe => recipe.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Default recipe ordering (name, identifier) and creator/visibility access.
            builder.HasIndex(recipe => new { recipe.Name, recipe.Id });
            builder.HasIndex(recipe => new { recipe.CreatedBy, recipe.Visibility, recipe.Id });
            // Category reference migration.
            builder.HasIndex(recipe => recipe.CategoryId);
            // Exact filters.
            builder.HasIndex(recipe => recipe.Visibility);
        });
    }

    private static void ConfigureRecipeIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecipeIngredient>(builder =>
        {
            builder.ToTable("recipe_ingredients");
            builder.HasKey(ingredient => ingredient.Id);
            builder.Property(ingredient => ingredient.Id).ValueGeneratedOnAdd();
            builder.Property(ingredient => ingredient.RecipeId).IsRequired();
            builder.Property(ingredient => ingredient.Name)
                .HasMaxLength(RecipesDefaults.IngredientNameMaximumLength).IsRequired();
            builder.Property(ingredient => ingredient.Quantity)
                .HasMaxLength(RecipesDefaults.IngredientQuantityMaximumLength);
            builder.Property(ingredient => ingredient.Position).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_recipe_ingredients_position",
                    "\"Position\" >= 0");
            });
            // Ordered ingredient retrieval and uniqueness per recipe.
            builder.HasIndex(ingredient => new { ingredient.RecipeId, ingredient.Position }).IsUnique();
            // Ingredient item-reference lookup for the Inventory deletion contract (Wave 4).
            builder.HasIndex(ingredient => ingredient.ItemId)
                .HasFilter("\"ItemId\" IS NOT NULL");
        });
    }

    private static void ConfigureRecipeStep(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecipeStep>(builder =>
        {
            builder.ToTable("recipe_steps");
            builder.HasKey(step => step.Id);
            builder.Property(step => step.Id).ValueGeneratedOnAdd();
            builder.Property(step => step.RecipeId).IsRequired();
            builder.Property(step => step.Instruction)
                .HasMaxLength(RecipesDefaults.StepInstructionMaximumLength).IsRequired();
            builder.Property(step => step.Position).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_recipe_steps_position",
                    "\"Position\" >= 0");
            });
            // Ordered step retrieval and uniqueness per recipe.
            builder.HasIndex(step => new { step.RecipeId, step.Position }).IsUnique();
        });
    }

    private static void ConfigureWeeklyMenu(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeeklyMenu>(builder =>
        {
            builder.ToTable("recipe_menus");
            builder.HasKey(menu => menu.Id);
            builder.Property(menu => menu.Id).ValueGeneratedOnAdd();
            builder.Property(menu => menu.Week).IsRequired();
            builder.Property(menu => menu.Name)
                .HasMaxLength(RecipesDefaults.MenuNameMaximumLength);
            builder.Property(menu => menu.Visibility)
                .HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(menu => menu.CreatedAt).IsRequired();
            builder.Property(menu => menu.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_recipe_menus_visibility",
                    "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasMany(menu => menu.SlotRecipes)
                .WithOne()
                .HasForeignKey(slot => slot.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(menu => menu.SlotRecipes)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(menu => menu.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>()
                .WithMany()
                .HasForeignKey(menu => menu.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            // Menu lookup by week.
            builder.HasIndex(menu => menu.Week);
            // Creator/visibility access.
            builder.HasIndex(menu => new { menu.CreatedBy, menu.Visibility, menu.Id });
        });
    }

    private static void ConfigureWeeklyMenuSlotRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeeklyMenuSlotRecipe>(builder =>
        {
            builder.ToTable("recipe_menu_slots");
            builder.HasKey(slot => new { slot.MenuId, slot.Day, slot.Slot, slot.RecipeId });
            builder.Property(slot => slot.Day).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(slot => slot.Slot).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_recipe_menu_slots_day",
                    "\"Day\" IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')");
                table.HasCheckConstraint(
                    "CK_recipe_menu_slots_slot",
                    "\"Slot\" IN ('Breakfast', 'Lunch', 'Snack', 'Dinner')");
            });
            builder.HasOne<Recipe>()
                .WithMany()
                .HasForeignKey(slot => slot.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
            // Recipe-deletion slot cleanup (Wave 5) and recipe-referenced-by-menu lookup.
            builder.HasIndex(slot => slot.RecipeId);
        });
    }
}
