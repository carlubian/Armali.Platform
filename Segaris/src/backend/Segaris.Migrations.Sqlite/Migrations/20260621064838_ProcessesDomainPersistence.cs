using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class ProcessesDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "processes_categories",
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
                table.PrimaryKey("PK_processes_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "processes_processes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                IsCancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_processes_processes", x => x.Id);
                table.CheckConstraint("CK_processes_processes_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_processes_processes_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_processes_processes_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_processes_processes_processes_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "processes_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "processes_steps",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProcessId = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                IsOptional = table.Column<bool>(type: "INTEGER", nullable: false),
                State = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_processes_steps", x => x.Id);
                table.CheckConstraint("CK_processes_steps_state", "\"State\" IN ('Pending', 'Completed', 'Skipped')");
                table.ForeignKey(
                    name: "FK_processes_steps_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_processes_steps_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_processes_steps_processes_processes_ProcessId",
                    column: x => x.ProcessId,
                    principalTable: "processes_processes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_processes_categories_NormalizedName",
            table: "processes_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_processes_categories_SortOrder",
            table: "processes_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_processes_processes_CategoryId",
            table: "processes_processes",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_processes_processes_CreatedBy_Visibility_Id",
            table: "processes_processes",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_processes_processes_DueDate_Id",
            table: "processes_processes",
            columns: new[] { "DueDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_processes_processes_IsCancelled_DueDate",
            table: "processes_processes",
            columns: new[] { "IsCancelled", "DueDate" });

        migrationBuilder.CreateIndex(
            name: "IX_processes_processes_UpdatedBy",
            table: "processes_processes",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_processes_processes_Visibility",
            table: "processes_processes",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_processes_steps_CreatedBy",
            table: "processes_steps",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_processes_steps_DueDate",
            table: "processes_steps",
            column: "DueDate");

        migrationBuilder.CreateIndex(
            name: "IX_processes_steps_ProcessId_SortOrder_Id",
            table: "processes_steps",
            columns: new[] { "ProcessId", "SortOrder", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_processes_steps_UpdatedBy",
            table: "processes_steps",
            column: "UpdatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "processes_steps");

        migrationBuilder.DropTable(
            name: "processes_processes");

        migrationBuilder.DropTable(
            name: "processes_categories");
    }
}
