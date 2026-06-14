using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class CapexDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "capex_categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_capex_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "capex_entries",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                MovementType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                CostCenterId = table.Column<int>(type: "INTEGER", nullable: true),
                CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_capex_entries", x => x.Id);
                table.CheckConstraint("CK_capex_entries_movement_type", "\"MovementType\" IN ('Income', 'Expense')");
                table.CheckConstraint("CK_capex_entries_status", "\"Status\" IN ('Planning', 'Completed', 'Canceled')");
                table.CheckConstraint("CK_capex_entries_total_amount", "\"TotalAmount\" >= 0");
                table.CheckConstraint("CK_capex_entries_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_capex_entries_capex_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "capex_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_capex_entries_configuration_cost_centers_CostCenterId",
                    column: x => x.CostCenterId,
                    principalTable: "configuration_cost_centers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_capex_entries_configuration_currencies_CurrencyId",
                    column: x => x.CurrencyId,
                    principalTable: "configuration_currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_capex_entries_configuration_suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "configuration_suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_capex_entries_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_capex_entries_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "capex_items",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                EntryId = table.Column<int>(type: "INTEGER", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                UnitAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                LineAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_capex_items", x => x.Id);
                table.CheckConstraint("CK_capex_items_line_amount", "\"LineAmount\" >= 0");
                table.CheckConstraint("CK_capex_items_position", "\"Position\" >= 0");
                table.CheckConstraint("CK_capex_items_quantity", "\"Quantity\" > 0");
                table.CheckConstraint("CK_capex_items_unit_amount", "\"UnitAmount\" >= 0");
                table.ForeignKey(
                    name: "FK_capex_items_capex_entries_EntryId",
                    column: x => x.EntryId,
                    principalTable: "capex_entries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_capex_categories_Code",
            table: "capex_categories",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_CategoryId",
            table: "capex_entries",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_CostCenterId",
            table: "capex_entries",
            column: "CostCenterId");

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_CreatedBy_Visibility_Id",
            table: "capex_entries",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_CurrencyId",
            table: "capex_entries",
            column: "CurrencyId");

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_DueDate_Id",
            table: "capex_entries",
            columns: new[] { "DueDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_Status_DueDate_Id",
            table: "capex_entries",
            columns: new[] { "Status", "DueDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_SupplierId",
            table: "capex_entries",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_capex_entries_UpdatedBy",
            table: "capex_entries",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_capex_items_EntryId_Position",
            table: "capex_items",
            columns: new[] { "EntryId", "Position" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "capex_items");

        migrationBuilder.DropTable(
            name: "capex_entries");

        migrationBuilder.DropTable(
            name: "capex_categories");
    }
}
