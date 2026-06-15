using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class CatalogModelAndInitialization : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_configuration_suppliers_Code",
            table: "configuration_suppliers");

        migrationBuilder.DropIndex(
            name: "IX_configuration_currencies_Code",
            table: "configuration_currencies");

        migrationBuilder.DropIndex(
            name: "IX_configuration_cost_centers_Code",
            table: "configuration_cost_centers");

        migrationBuilder.DropIndex(
            name: "IX_capex_categories_Code",
            table: "capex_categories");

        migrationBuilder.DropColumn(
            name: "Code",
            table: "configuration_suppliers");

        migrationBuilder.DropColumn(
            name: "Code",
            table: "configuration_cost_centers");

        migrationBuilder.DropColumn(
            name: "Code",
            table: "capex_categories");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "configuration_suppliers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(120)",
            oldMaxLength: 120);

        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "configuration_suppliers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "SortOrder",
            table: "configuration_suppliers",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "configuration_currencies",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(120)",
            oldMaxLength: 120);

        migrationBuilder.AddColumn<string>(
            name: "NormalizedCode",
            table: "configuration_currencies",
            type: "character(3)",
            fixedLength: true,
            maxLength: 3,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "configuration_currencies",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "SortOrder",
            table: "configuration_currencies",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "configuration_cost_centers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(120)",
            oldMaxLength: 120);

        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "configuration_cost_centers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "SortOrder",
            table: "configuration_cost_centers",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "capex_categories",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(120)",
            oldMaxLength: 120);

        migrationBuilder.AddColumn<string>(
            name: "NormalizedName",
            table: "capex_categories",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "SortOrder",
            table: "capex_categories",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "configuration_catalog_initializations",
            columns: table => new
            {
                CatalogKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                InitializedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_configuration_catalog_initializations", x => x.CatalogKey);
            });

        // Backfill deterministic order and normalized uniqueness values for any
        // rows that already exist (an upgrade). On a fresh database the catalog
        // tables are still empty here, so these statements affect no rows and the
        // unique indexes created below start clean. PostgreSQL's UPPER is the
        // production reference for case folding.
        BackfillCatalog(migrationBuilder, "configuration_suppliers");
        BackfillCatalog(migrationBuilder, "configuration_cost_centers");
        BackfillCatalog(migrationBuilder, "capex_categories");
        migrationBuilder.Sql(
            "UPDATE configuration_currencies SET \"NormalizedName\" = UPPER(TRIM(\"Name\")), " +
            "\"NormalizedCode\" = UPPER(TRIM(\"Code\"));");
        BackfillSortOrder(migrationBuilder, "configuration_currencies");

        // Mark every catalog that already contains rows as initialized so startup
        // seeding never restores or duplicates an existing or customized catalog.
        // Empty catalogs (a fresh install) are intentionally left unmarked so the
        // application seeds them once.
        MarkInitialized(migrationBuilder, "configuration.suppliers", "configuration_suppliers");
        MarkInitialized(migrationBuilder, "configuration.cost-centers", "configuration_cost_centers");
        MarkInitialized(migrationBuilder, "configuration.currencies", "configuration_currencies");
        MarkInitialized(migrationBuilder, "capex.categories", "capex_categories");

        migrationBuilder.CreateIndex(
            name: "IX_configuration_suppliers_NormalizedName",
            table: "configuration_suppliers",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_configuration_suppliers_SortOrder",
            table: "configuration_suppliers",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_configuration_currencies_NormalizedCode",
            table: "configuration_currencies",
            column: "NormalizedCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_configuration_currencies_NormalizedName",
            table: "configuration_currencies",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_configuration_currencies_SortOrder",
            table: "configuration_currencies",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_configuration_cost_centers_NormalizedName",
            table: "configuration_cost_centers",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_configuration_cost_centers_SortOrder",
            table: "configuration_cost_centers",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_capex_categories_NormalizedName",
            table: "capex_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_capex_categories_SortOrder",
            table: "capex_categories",
            column: "SortOrder");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "configuration_catalog_initializations");

        migrationBuilder.DropIndex(
            name: "IX_configuration_suppliers_NormalizedName",
            table: "configuration_suppliers");

        migrationBuilder.DropIndex(
            name: "IX_configuration_suppliers_SortOrder",
            table: "configuration_suppliers");

        migrationBuilder.DropIndex(
            name: "IX_configuration_currencies_NormalizedCode",
            table: "configuration_currencies");

        migrationBuilder.DropIndex(
            name: "IX_configuration_currencies_NormalizedName",
            table: "configuration_currencies");

        migrationBuilder.DropIndex(
            name: "IX_configuration_currencies_SortOrder",
            table: "configuration_currencies");

        migrationBuilder.DropIndex(
            name: "IX_configuration_cost_centers_NormalizedName",
            table: "configuration_cost_centers");

        migrationBuilder.DropIndex(
            name: "IX_configuration_cost_centers_SortOrder",
            table: "configuration_cost_centers");

        migrationBuilder.DropIndex(
            name: "IX_capex_categories_NormalizedName",
            table: "capex_categories");

        migrationBuilder.DropIndex(
            name: "IX_capex_categories_SortOrder",
            table: "capex_categories");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            table: "configuration_suppliers");

        migrationBuilder.DropColumn(
            name: "SortOrder",
            table: "configuration_suppliers");

        migrationBuilder.DropColumn(
            name: "NormalizedCode",
            table: "configuration_currencies");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            table: "configuration_currencies");

        migrationBuilder.DropColumn(
            name: "SortOrder",
            table: "configuration_currencies");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            table: "configuration_cost_centers");

        migrationBuilder.DropColumn(
            name: "SortOrder",
            table: "configuration_cost_centers");

        migrationBuilder.DropColumn(
            name: "NormalizedName",
            table: "capex_categories");

        migrationBuilder.DropColumn(
            name: "SortOrder",
            table: "capex_categories");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "configuration_suppliers",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AddColumn<string>(
            name: "Code",
            table: "configuration_suppliers",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "configuration_currencies",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "configuration_cost_centers",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AddColumn<string>(
            name: "Code",
            table: "configuration_cost_centers",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "capex_categories",
            type: "character varying(120)",
            maxLength: 120,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AddColumn<string>(
            name: "Code",
            table: "capex_categories",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_configuration_suppliers_Code",
            table: "configuration_suppliers",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_configuration_currencies_Code",
            table: "configuration_currencies",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_configuration_cost_centers_Code",
            table: "configuration_cost_centers",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_capex_categories_Code",
            table: "capex_categories",
            column: "Code",
            unique: true);
    }

    private static void BackfillCatalog(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($"UPDATE {table} SET \"NormalizedName\" = UPPER(TRIM(\"Name\"));");
        BackfillSortOrder(migrationBuilder, table);
    }

    private static void BackfillSortOrder(MigrationBuilder migrationBuilder, string table)
    {
        // Existing rows receive a zero-based SortOrder following ascending Id.
        migrationBuilder.Sql(
            $"UPDATE {table} AS target SET \"SortOrder\" = ordered.rank " +
            "FROM (SELECT \"Id\", (ROW_NUMBER() OVER (ORDER BY \"Id\") - 1) AS rank " +
            $"FROM {table}) AS ordered WHERE target.\"Id\" = ordered.\"Id\";");
    }

    private static void MarkInitialized(MigrationBuilder migrationBuilder, string catalogKey, string table)
    {
        migrationBuilder.Sql(
            "INSERT INTO configuration_catalog_initializations (\"CatalogKey\", \"InitializedAt\") " +
            $"SELECT '{catalogKey}', now() " +
            $"WHERE EXISTS (SELECT 1 FROM {table});");
    }
}
