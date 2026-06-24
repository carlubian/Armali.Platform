using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

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
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Visibility = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: false)
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
