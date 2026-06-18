using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class MoodDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "mood_entries",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                EntryDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: false),
                Energy = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Alignment = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Direction = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_mood_entries", x => x.Id);
                table.CheckConstraint("CK_mood_entries_alignment", "\"Alignment\" IN ('Negative', 'Medium', 'Positive')");
                table.CheckConstraint("CK_mood_entries_direction", "\"Direction\" IN ('Harmony', 'Defensive', 'Offensive', 'Stability')");
                table.CheckConstraint("CK_mood_entries_energy", "\"Energy\" IN ('Low', 'Medium', 'High')");
                table.CheckConstraint("CK_mood_entries_score", "\"Score\" >= 1 AND \"Score\" <= 5");
                table.CheckConstraint("CK_mood_entries_source", "\"Source\" IN ('Internal', 'External')");
                table.ForeignKey(
                    name: "FK_mood_entries_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_mood_entries_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_mood_entries_CreatedBy_EntryDate",
            table: "mood_entries",
            columns: new[] { "CreatedBy", "EntryDate" });

        migrationBuilder.CreateIndex(
            name: "IX_mood_entries_CreatedBy_EntryDate_Id",
            table: "mood_entries",
            columns: new[] { "CreatedBy", "EntryDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_mood_entries_CreatedBy_Id",
            table: "mood_entries",
            columns: new[] { "CreatedBy", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_mood_entries_UpdatedBy",
            table: "mood_entries",
            column: "UpdatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "mood_entries");
    }
}
