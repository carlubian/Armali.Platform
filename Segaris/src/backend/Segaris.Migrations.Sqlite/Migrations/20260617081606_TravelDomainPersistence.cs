using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class TravelDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "travel_expense_categories",
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
                table.PrimaryKey("PK_travel_expense_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "travel_trip_types",
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
                table.PrimaryKey("PK_travel_trip_types", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "travel_trips",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                TripTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                Destination = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_travel_trips", x => x.Id);
                table.CheckConstraint("CK_travel_trips_dates", "\"EndDate\" >= \"StartDate\"");
                table.CheckConstraint("CK_travel_trips_status", "\"Status\" IN ('Planned', 'Ongoing', 'Completed', 'Cancelled')");
                table.CheckConstraint("CK_travel_trips_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_travel_trips_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_trips_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_trips_travel_trip_types_TripTypeId",
                    column: x => x.TripTypeId,
                    principalTable: "travel_trip_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "travel_expenses",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TripId = table.Column<int>(type: "INTEGER", nullable: false),
                ExpenseCategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                CurrencyId = table.Column<int>(type: "INTEGER", nullable: false),
                SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                CostCenterId = table.Column<int>(type: "INTEGER", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_travel_expenses", x => x.Id);
                table.CheckConstraint("CK_travel_expenses_amount", "\"Amount\" >= 0");
                table.ForeignKey(
                    name: "FK_travel_expenses_configuration_cost_centers_CostCenterId",
                    column: x => x.CostCenterId,
                    principalTable: "configuration_cost_centers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_expenses_configuration_currencies_CurrencyId",
                    column: x => x.CurrencyId,
                    principalTable: "configuration_currencies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_expenses_configuration_suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalTable: "configuration_suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_expenses_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_expenses_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_expenses_travel_expense_categories_ExpenseCategoryId",
                    column: x => x.ExpenseCategoryId,
                    principalTable: "travel_expense_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_travel_expenses_travel_trips_TripId",
                    column: x => x.TripId,
                    principalTable: "travel_trips",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "travel_itinerary_entries",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TripId = table.Column<int>(type: "INTEGER", nullable: false),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Time = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Place = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ReservationLocator = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_travel_itinerary_entries", x => x.Id);
                table.ForeignKey(
                    name: "FK_travel_itinerary_entries_travel_trips_TripId",
                    column: x => x.TripId,
                    principalTable: "travel_trips",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_travel_expense_categories_NormalizedName",
            table: "travel_expense_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_travel_expense_categories_SortOrder",
            table: "travel_expense_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_CostCenterId",
            table: "travel_expenses",
            column: "CostCenterId");

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_CreatedBy",
            table: "travel_expenses",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_CurrencyId",
            table: "travel_expenses",
            column: "CurrencyId");

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_ExpenseCategoryId",
            table: "travel_expenses",
            column: "ExpenseCategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_SupplierId",
            table: "travel_expenses",
            column: "SupplierId");

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_TripId_CurrencyId",
            table: "travel_expenses",
            columns: new[] { "TripId", "CurrencyId" });

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_TripId_Id",
            table: "travel_expenses",
            columns: new[] { "TripId", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_travel_expenses_UpdatedBy",
            table: "travel_expenses",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_travel_itinerary_entries_TripId_Date_Time_SortOrder",
            table: "travel_itinerary_entries",
            columns: new[] { "TripId", "Date", "Time", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_travel_trip_types_NormalizedName",
            table: "travel_trip_types",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_travel_trip_types_SortOrder",
            table: "travel_trip_types",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_travel_trips_CreatedBy_Visibility_Id",
            table: "travel_trips",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_travel_trips_StartDate_Id",
            table: "travel_trips",
            columns: new[] { "StartDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_travel_trips_Status_StartDate",
            table: "travel_trips",
            columns: new[] { "Status", "StartDate" });

        migrationBuilder.CreateIndex(
            name: "IX_travel_trips_TripTypeId",
            table: "travel_trips",
            column: "TripTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_travel_trips_UpdatedBy",
            table: "travel_trips",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_travel_trips_Visibility",
            table: "travel_trips",
            column: "Visibility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "travel_expenses");

        migrationBuilder.DropTable(
            name: "travel_itinerary_entries");

        migrationBuilder.DropTable(
            name: "travel_expense_categories");

        migrationBuilder.DropTable(
            name: "travel_trips");

        migrationBuilder.DropTable(
            name: "travel_trip_types");
    }
}
