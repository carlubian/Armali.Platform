using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class AssetsDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "asset_categories",
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
                table.PrimaryKey("PK_asset_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "asset_locations",
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
                table.PrimaryKey("PK_asset_locations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "assets",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                NormalizedCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                BrandModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                SerialNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                AcquisitionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ExpectedEndOfLifeDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
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
                table.PrimaryKey("PK_assets", x => x.Id);
                table.CheckConstraint("CK_assets_status", "\"Status\" IN ('Active', 'Stored', 'Retired')");
                table.CheckConstraint("CK_assets_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_assets_asset_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "asset_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_assets_asset_locations_LocationId",
                    column: x => x.LocationId,
                    principalTable: "asset_locations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_assets_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_assets_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_asset_categories_NormalizedName",
            table: "asset_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_asset_categories_SortOrder",
            table: "asset_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_asset_locations_NormalizedName",
            table: "asset_locations",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_asset_locations_SortOrder",
            table: "asset_locations",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_assets_CategoryId",
            table: "assets",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_assets_CreatedBy_Visibility_Id",
            table: "assets",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_assets_LocationId",
            table: "assets",
            column: "LocationId");

        migrationBuilder.CreateIndex(
            name: "IX_assets_Name_Id",
            table: "assets",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_assets_NormalizedCode",
            table: "assets",
            column: "NormalizedCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_assets_Status_ExpectedEndOfLifeDate",
            table: "assets",
            columns: new[] { "Status", "ExpectedEndOfLifeDate" });

        migrationBuilder.CreateIndex(
            name: "IX_assets_UpdatedBy",
            table: "assets",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_assets_Visibility",
            table: "assets",
            column: "Visibility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "assets");

        migrationBuilder.DropTable(
            name: "asset_categories");

        migrationBuilder.DropTable(
            name: "asset_locations");
    }
}
