using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class CalendarDailyNotes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "calendar_daily_notes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_calendar_daily_notes", x => x.Id);
                table.CheckConstraint("CK_calendar_daily_notes_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_calendar_daily_notes_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_calendar_daily_notes_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_calendar_daily_notes_CreatedBy_Id",
            table: "calendar_daily_notes",
            columns: new[] { "CreatedBy", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_calendar_daily_notes_CreatedBy_Visibility_Date_Id",
            table: "calendar_daily_notes",
            columns: new[] { "CreatedBy", "Visibility", "Date", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_calendar_daily_notes_Date_Id",
            table: "calendar_daily_notes",
            columns: new[] { "Date", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_calendar_daily_notes_UpdatedBy",
            table: "calendar_daily_notes",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_calendar_daily_notes_Visibility_Date_Id",
            table: "calendar_daily_notes",
            columns: new[] { "Visibility", "Date", "Id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "calendar_daily_notes");
    }
}
