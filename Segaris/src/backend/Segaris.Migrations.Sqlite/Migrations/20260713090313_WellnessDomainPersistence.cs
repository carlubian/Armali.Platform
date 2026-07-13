using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class WellnessDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "wellness_days",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wellness_days", x => x.Id);
                table.CheckConstraint("CK_wellness_days_score", "\"Score\" IS NULL OR (\"Score\" BETWEEN 0 AND 100)");
                table.ForeignKey(
                    name: "FK_wellness_days_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_wellness_days_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "wellness_tasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wellness_tasks", x => x.Id);
                table.CheckConstraint("CK_wellness_tasks_category", "\"Category\" IN ('HealthAndBody', 'MindAndSleep', 'PeopleAndWork')");
                table.ForeignKey(
                    name: "FK_wellness_tasks_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_wellness_tasks_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "wellness_day_tasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                WellnessDayId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                Completed = table.Column<bool>(type: "INTEGER", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wellness_day_tasks", x => x.Id);
                table.CheckConstraint("CK_wellness_day_tasks_category", "\"Category\" IN ('HealthAndBody', 'MindAndSleep', 'PeopleAndWork')");
                table.ForeignKey(
                    name: "FK_wellness_day_tasks_wellness_days_WellnessDayId",
                    column: x => x.WellnessDayId,
                    principalTable: "wellness_days",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_wellness_day_tasks_WellnessDayId_Position_Id",
            table: "wellness_day_tasks",
            columns: new[] { "WellnessDayId", "Position", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_wellness_days_CreatedBy_Date",
            table: "wellness_days",
            columns: new[] { "CreatedBy", "Date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_wellness_days_CreatedBy_Date_Id",
            table: "wellness_days",
            columns: new[] { "CreatedBy", "Date", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_wellness_days_UpdatedBy",
            table: "wellness_days",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_wellness_tasks_CreatedBy",
            table: "wellness_tasks",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_wellness_tasks_SortOrder_Id",
            table: "wellness_tasks",
            columns: new[] { "SortOrder", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_wellness_tasks_UpdatedBy",
            table: "wellness_tasks",
            column: "UpdatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "wellness_day_tasks");

        migrationBuilder.DropTable(
            name: "wellness_tasks");

        migrationBuilder.DropTable(
            name: "wellness_days");
    }
}
