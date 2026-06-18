using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class ClothesDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "clothing_categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clothing_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "clothing_colors",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ColorValue = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clothing_colors", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "clothes_garments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Size = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                WashingCare = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                DryingCare = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                IroningCare = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                DryCleaningCare = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                PrimaryAttachmentId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clothes_garments", x => x.Id);
                table.CheckConstraint("CK_clothes_garments_dry_cleaning_care", "\"DryCleaningCare\" IS NULL OR \"DryCleaningCare\" IN ('Any', 'DoNotDryClean')");
                table.CheckConstraint("CK_clothes_garments_drying_care", "\"DryingCare\" IS NULL OR \"DryingCare\" IN ('Any', 'Delicate', 'VeryDelicate')");
                table.CheckConstraint("CK_clothes_garments_ironing_care", "\"IroningCare\" IS NULL OR \"IroningCare\" IN ('Any', 'Low', 'Medium', 'DoNotIron')");
                table.CheckConstraint("CK_clothes_garments_status", "\"Status\" IN ('Active', 'Unavailable', 'Deprecated')");
                table.CheckConstraint("CK_clothes_garments_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.CheckConstraint("CK_clothes_garments_washing_care", "\"WashingCare\" IS NULL OR \"WashingCare\" IN ('Any', 'Wash30', 'Wash30Delicate', 'Wash40', 'Wash40Delicate', 'Wash50', 'Wash50Delicate', 'Wash60', 'Wash60Delicate', 'HandWash', 'DoNotWash')");
                table.ForeignKey(
                    name: "FK_clothes_garments_clothing_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "clothing_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_clothes_garments_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_clothes_garments_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "clothes_garment_colors",
            columns: table => new
            {
                GarmentId = table.Column<int>(type: "INTEGER", nullable: false),
                ColorId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_clothes_garment_colors", x => new { x.GarmentId, x.ColorId });
                table.ForeignKey(
                    name: "FK_clothes_garment_colors_clothes_garments_GarmentId",
                    column: x => x.GarmentId,
                    principalTable: "clothes_garments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_clothes_garment_colors_clothing_colors_ColorId",
                    column: x => x.ColorId,
                    principalTable: "clothing_colors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garment_colors_ColorId",
            table: "clothes_garment_colors",
            column: "ColorId");

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garments_CategoryId",
            table: "clothes_garments",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garments_CreatedBy_Visibility_Id",
            table: "clothes_garments",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garments_Name_Id",
            table: "clothes_garments",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garments_Status",
            table: "clothes_garments",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garments_UpdatedBy",
            table: "clothes_garments",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_clothes_garments_Visibility",
            table: "clothes_garments",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_clothing_categories_NormalizedName",
            table: "clothing_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_clothing_categories_SortOrder",
            table: "clothing_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_clothing_colors_NormalizedName",
            table: "clothing_colors",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_clothing_colors_SortOrder",
            table: "clothing_colors",
            column: "SortOrder");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "clothes_garment_colors");

        migrationBuilder.DropTable(
            name: "clothes_garments");

        migrationBuilder.DropTable(
            name: "clothing_colors");

        migrationBuilder.DropTable(
            name: "clothing_categories");
    }
}
