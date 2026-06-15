using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class OpexDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "opex_categories",
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
                table.PrimaryKey("PK_opex_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "opex_contracts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                MovementType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ClosedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                EstimatedAnnualAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                ExpectedFrequency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                CostCenterId = table.Column<int>(type: "INTEGER", nullable: true),
                CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_opex_contracts", x => x.Id);
                table.CheckConstraint("CK_opex_contracts_estimated_annual_amount", "\"EstimatedAnnualAmount\" IS NULL OR \"EstimatedAnnualAmount\" >= 0");
                table.CheckConstraint("CK_opex_contracts_frequency", "\"ExpectedFrequency\" IN ('None', 'Weekly', 'Monthly', 'Quarterly', 'SemiAnnual', 'Annual', 'Irregular')");
                table.CheckConstraint("CK_opex_contracts_movement_type", "\"MovementType\" IN ('Income', 'Expense')");
                table.CheckConstraint("CK_opex_contracts_status", "\"Status\" IN ('Planning', 'Active', 'OnHold', 'Closed')");
                table.CheckConstraint("CK_opex_contracts_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_opex_contracts_configuration_cost_centers_CostCenterId",
                    column: x => x.CostCenterId,
                    principalTable: "configuration_cost_centers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_contracts_configuration_currencies_CurrencyId",
                    column: x => x.CurrencyId,
                    principalTable: "configuration_currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_contracts_configuration_suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "configuration_suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_contracts_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_contracts_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_contracts_opex_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "opex_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "opex_occurrences",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                ActualAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_opex_occurrences", x => x.Id);
                table.CheckConstraint("CK_opex_occurrences_actual_amount", "\"ActualAmount\" >= 0");
                table.ForeignKey(
                    name: "FK_opex_occurrences_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_occurrences_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_opex_occurrences_opex_contracts_ContractId",
                    column: x => x.ContractId,
                    principalTable: "opex_contracts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_opex_categories_NormalizedName",
            table: "opex_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_opex_categories_SortOrder",
            table: "opex_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_CategoryId",
            table: "opex_contracts",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_CostCenterId",
            table: "opex_contracts",
            column: "CostCenterId");

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_CreatedBy_Visibility_Id",
            table: "opex_contracts",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_CurrencyId",
            table: "opex_contracts",
            column: "CurrencyId");

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_Name_Id",
            table: "opex_contracts",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_NormalizedName",
            table: "opex_contracts",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_SupplierId",
            table: "opex_contracts",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_opex_contracts_UpdatedBy",
            table: "opex_contracts",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_opex_occurrences_ContractId_EffectiveDate_Id",
            table: "opex_occurrences",
            columns: new[] { "ContractId", "EffectiveDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_opex_occurrences_CreatedBy",
            table: "opex_occurrences",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_opex_occurrences_UpdatedBy",
            table: "opex_occurrences",
            column: "UpdatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "opex_occurrences");

        migrationBuilder.DropTable(
            name: "opex_contracts");

        migrationBuilder.DropTable(
            name: "opex_categories");
    }
}
