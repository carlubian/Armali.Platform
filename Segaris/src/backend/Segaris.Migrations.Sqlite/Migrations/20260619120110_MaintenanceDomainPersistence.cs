using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class MaintenanceDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "maintenance_types",
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
                table.PrimaryKey("PK_maintenance_types", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "maintenance_tasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                MaintenanceTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Priority = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                CompletedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                AssetId = table.Column<int>(type: "INTEGER", nullable: true),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_maintenance_tasks", x => x.Id);
                table.CheckConstraint("CK_maintenance_tasks_priority", "\"Priority\" IN ('Low', 'Medium', 'High')");
                table.CheckConstraint("CK_maintenance_tasks_status", "\"Status\" IN ('Pending', 'InProgress', 'Completed', 'Cancelled')");
                table.CheckConstraint("CK_maintenance_tasks_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_maintenance_tasks_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_maintenance_tasks_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_maintenance_tasks_maintenance_types_MaintenanceTypeId",
                    column: x => x.MaintenanceTypeId,
                    principalTable: "maintenance_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_AssetId",
            table: "maintenance_tasks",
            column: "AssetId");

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_CreatedBy_Visibility_Id",
            table: "maintenance_tasks",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_DueDate_Id",
            table: "maintenance_tasks",
            columns: new[] { "DueDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_MaintenanceTypeId",
            table: "maintenance_tasks",
            column: "MaintenanceTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_Priority",
            table: "maintenance_tasks",
            column: "Priority");

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_Status_DueDate",
            table: "maintenance_tasks",
            columns: new[] { "Status", "DueDate" });

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_UpdatedBy",
            table: "maintenance_tasks",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_tasks_Visibility",
            table: "maintenance_tasks",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_types_NormalizedName",
            table: "maintenance_types",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_types_SortOrder",
            table: "maintenance_types",
            column: "SortOrder");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "maintenance_tasks");

        migrationBuilder.DropTable(
            name: "maintenance_types");
    }
}
