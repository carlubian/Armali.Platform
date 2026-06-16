using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class InventoryDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "inventory_categories",
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
                table.PrimaryKey("PK_inventory_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "inventory_locations",
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
                table.PrimaryKey("PK_inventory_locations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "inventory_orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                OrderDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                ExpectedReceiptDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_orders", x => x.Id);
                table.CheckConstraint("CK_inventory_orders_status", "\"Status\" IN ('Planning', 'Active', 'Received', 'Cancelled')");
                table.CheckConstraint("CK_inventory_orders_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_inventory_orders_configuration_currencies_CurrencyId",
                    column: x => x.CurrencyId,
                    principalTable: "configuration_currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_orders_configuration_suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "configuration_suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_orders_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_orders_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_items",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                CurrentStock = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                MinimumStock = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_items", x => x.Id);
                table.CheckConstraint("CK_inventory_items_current_stock", "\"CurrentStock\" >= 0");
                table.CheckConstraint("CK_inventory_items_minimum_stock", "\"MinimumStock\" >= 0");
                table.CheckConstraint("CK_inventory_items_status", "\"Status\" IN ('Candidate', 'Active', 'Deprecated')");
                table.CheckConstraint("CK_inventory_items_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_inventory_items_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_items_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_items_inventory_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "inventory_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_items_inventory_locations_LocationId",
                    column: x => x.LocationId,
                    principalTable: "inventory_locations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_item_suppliers",
            columns: table => new
            {
                ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_item_suppliers", x => new { x.ItemId, x.SupplierId });
                table.ForeignKey(
                    name: "FK_inventory_item_suppliers_configuration_suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "configuration_suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_item_suppliers_inventory_items_ItemId",
                    column: x => x.ItemId,
                    principalTable: "inventory_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "inventory_order_lines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inventory_order_lines", x => x.Id);
                table.CheckConstraint("CK_inventory_order_lines_line_total", "\"LineTotal\" >= 0");
                table.CheckConstraint("CK_inventory_order_lines_quantity", "\"Quantity\" > 0");
                table.ForeignKey(
                    name: "FK_inventory_order_lines_inventory_items_ItemId",
                    column: x => x.ItemId,
                    principalTable: "inventory_items",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_inventory_order_lines_inventory_orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "inventory_orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_categories_NormalizedName",
            table: "inventory_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_inventory_categories_SortOrder",
            table: "inventory_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_item_suppliers_SupplierId",
            table: "inventory_item_suppliers",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_CategoryId",
            table: "inventory_items",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_CreatedBy_Visibility_Id",
            table: "inventory_items",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_LocationId",
            table: "inventory_items",
            column: "LocationId");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_Name_Id",
            table: "inventory_items",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_Status_Visibility",
            table: "inventory_items",
            columns: new[] { "Status", "Visibility" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_UpdatedBy",
            table: "inventory_items",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_items_Visibility",
            table: "inventory_items",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_locations_NormalizedName",
            table: "inventory_locations",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_inventory_locations_SortOrder",
            table: "inventory_locations",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_order_lines_ItemId",
            table: "inventory_order_lines",
            column: "ItemId");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_order_lines_OrderId_Id",
            table: "inventory_order_lines",
            columns: new[] { "OrderId", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_CreatedBy_Visibility_Id",
            table: "inventory_orders",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_CurrencyId",
            table: "inventory_orders",
            column: "CurrencyId");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_OrderDate_Id",
            table: "inventory_orders",
            columns: new[] { "OrderDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_Status",
            table: "inventory_orders",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_SupplierId",
            table: "inventory_orders",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_UpdatedBy",
            table: "inventory_orders",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_inventory_orders_Visibility",
            table: "inventory_orders",
            column: "Visibility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "inventory_item_suppliers");

        migrationBuilder.DropTable(
            name: "inventory_order_lines");

        migrationBuilder.DropTable(
            name: "inventory_items");

        migrationBuilder.DropTable(
            name: "inventory_orders");

        migrationBuilder.DropTable(
            name: "inventory_categories");

        migrationBuilder.DropTable(
            name: "inventory_locations");
    }
}
