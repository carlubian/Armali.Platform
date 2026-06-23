using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class HealthDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "health_disease_categories",
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
                table.PrimaryKey("PK_health_disease_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "health_medicine_categories",
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
                table.PrimaryKey("PK_health_medicine_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "health_diseases",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Symptoms = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                AverageDurationDays = table.Column<int>(type: "INTEGER", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_health_diseases", x => x.Id);
                table.CheckConstraint("CK_health_diseases_average_duration_days", "\"AverageDurationDays\" IS NULL OR \"AverageDurationDays\" BETWEEN 1 AND 100000");
                table.CheckConstraint("CK_health_diseases_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_health_diseases_health_disease_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "health_disease_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_health_diseases_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_health_diseases_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "health_medicines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                Posology = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                RequiresPrescription = table.Column<bool>(type: "INTEGER", nullable: false),
                InventoryItemId = table.Column<int>(type: "INTEGER", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_health_medicines", x => x.Id);
                table.CheckConstraint("CK_health_medicines_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_health_medicines_health_medicine_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "health_medicine_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_health_medicines_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_health_medicines_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "health_disease_medicines",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DiseaseId = table.Column<int>(type: "INTEGER", nullable: false),
                MedicineId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_health_disease_medicines", x => x.Id);
                table.ForeignKey(
                    name: "FK_health_disease_medicines_health_diseases_DiseaseId",
                    column: x => x.DiseaseId,
                    principalTable: "health_diseases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_health_disease_medicines_health_medicines_MedicineId",
                    column: x => x.MedicineId,
                    principalTable: "health_medicines",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_health_disease_categories_NormalizedName",
            table: "health_disease_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_health_disease_categories_SortOrder",
            table: "health_disease_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_health_disease_medicines_DiseaseId_MedicineId",
            table: "health_disease_medicines",
            columns: new[] { "DiseaseId", "MedicineId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_health_disease_medicines_MedicineId",
            table: "health_disease_medicines",
            column: "MedicineId");

        migrationBuilder.CreateIndex(
            name: "IX_health_diseases_CategoryId",
            table: "health_diseases",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_health_diseases_CreatedBy_Visibility_Id",
            table: "health_diseases",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_health_diseases_Name_Id",
            table: "health_diseases",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_health_diseases_UpdatedBy",
            table: "health_diseases",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_health_diseases_Visibility",
            table: "health_diseases",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_health_medicine_categories_NormalizedName",
            table: "health_medicine_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_health_medicine_categories_SortOrder",
            table: "health_medicine_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_CategoryId",
            table: "health_medicines",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_CreatedBy_Visibility_Id",
            table: "health_medicines",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_InventoryItemId",
            table: "health_medicines",
            column: "InventoryItemId",
            filter: "\"InventoryItemId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_Name_Id",
            table: "health_medicines",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_RequiresPrescription",
            table: "health_medicines",
            column: "RequiresPrescription");

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_UpdatedBy",
            table: "health_medicines",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_health_medicines_Visibility",
            table: "health_medicines",
            column: "Visibility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "health_disease_medicines");

        migrationBuilder.DropTable(
            name: "health_diseases");

        migrationBuilder.DropTable(
            name: "health_medicines");

        migrationBuilder.DropTable(
            name: "health_disease_categories");

        migrationBuilder.DropTable(
            name: "health_medicine_categories");
    }
}
