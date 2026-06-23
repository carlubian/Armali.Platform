using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class MedicinePrimaryAttachment : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PrimaryAttachmentId",
            table: "health_medicines",
            type: "integer",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PrimaryAttachmentId",
            table: "health_medicines");
    }
}
