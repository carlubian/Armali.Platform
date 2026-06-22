using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class RecipesDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "recipe_categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "recipe_menus",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Week = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_menus", x => x.Id);
                table.CheckConstraint("CK_recipe_menus_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_recipe_menus_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipe_menus_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "recipes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Difficulty = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                Servings = table.Column<int>(type: "INTEGER", nullable: true),
                PreparationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                CookMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipes", x => x.Id);
                table.CheckConstraint("CK_recipes_cook_minutes", "\"CookMinutes\" IS NULL OR \"CookMinutes\" > 0");
                table.CheckConstraint("CK_recipes_difficulty", "\"Difficulty\" IS NULL OR \"Difficulty\" IN ('Easy', 'Medium', 'Hard')");
                table.CheckConstraint("CK_recipes_preparation_minutes", "\"PreparationMinutes\" IS NULL OR \"PreparationMinutes\" > 0");
                table.CheckConstraint("CK_recipes_servings", "\"Servings\" IS NULL OR \"Servings\" > 0");
                table.CheckConstraint("CK_recipes_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_recipes_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipes_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipes_recipe_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "recipe_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "recipe_ingredients",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Quantity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                Position = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_ingredients", x => x.Id);
                table.CheckConstraint("CK_recipe_ingredients_position", "\"Position\" >= 0");
                table.ForeignKey(
                    name: "FK_recipe_ingredients_recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipe_menu_slots",
            columns: table => new
            {
                MenuId = table.Column<int>(type: "INTEGER", nullable: false),
                Day = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Slot = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                RecipeId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_menu_slots", x => new { x.MenuId, x.Day, x.Slot, x.RecipeId });
                table.CheckConstraint("CK_recipe_menu_slots_day", "\"Day\" IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')");
                table.CheckConstraint("CK_recipe_menu_slots_slot", "\"Slot\" IN ('Breakfast', 'Lunch', 'Snack', 'Dinner')");
                table.ForeignKey(
                    name: "FK_recipe_menu_slots_recipe_menus_MenuId",
                    column: x => x.MenuId,
                    principalTable: "recipe_menus",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_recipe_menu_slots_recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "recipe_steps",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                Instruction = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_steps", x => x.Id);
                table.CheckConstraint("CK_recipe_steps_position", "\"Position\" >= 0");
                table.ForeignKey(
                    name: "FK_recipe_steps_recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_recipe_categories_NormalizedName",
            table: "recipe_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipe_categories_SortOrder",
            table: "recipe_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredients_ItemId",
            table: "recipe_ingredients",
            column: "ItemId",
            filter: "\"ItemId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredients_RecipeId_Position",
            table: "recipe_ingredients",
            columns: new[] { "RecipeId", "Position" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipe_menu_slots_RecipeId",
            table: "recipe_menu_slots",
            column: "RecipeId");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_menus_CreatedBy_Visibility_Id",
            table: "recipe_menus",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_recipe_menus_UpdatedBy",
            table: "recipe_menus",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_menus_Week",
            table: "recipe_menus",
            column: "Week");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_steps_RecipeId_Position",
            table: "recipe_steps",
            columns: new[] { "RecipeId", "Position" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_recipes_CategoryId",
            table: "recipes",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_recipes_CreatedBy_Visibility_Id",
            table: "recipes",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_recipes_Name_Id",
            table: "recipes",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_recipes_UpdatedBy",
            table: "recipes",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_recipes_Visibility",
            table: "recipes",
            column: "Visibility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "recipe_ingredients");

        migrationBuilder.DropTable(
            name: "recipe_menu_slots");

        migrationBuilder.DropTable(
            name: "recipe_steps");

        migrationBuilder.DropTable(
            name: "recipe_menus");

        migrationBuilder.DropTable(
            name: "recipes");

        migrationBuilder.DropTable(
            name: "recipe_categories");
    }
}
