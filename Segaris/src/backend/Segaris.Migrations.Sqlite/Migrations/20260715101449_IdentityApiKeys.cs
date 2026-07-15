using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class IdentityApiKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "identity_api_keys",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                KeyId = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                SecretHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                SecurityStamp = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastUsedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_identity_api_keys", x => x.Id);
                table.ForeignKey(
                    name: "FK_identity_api_keys_identity_users_UserId",
                    column: x => x.UserId,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_identity_api_keys_KeyId",
            table: "identity_api_keys",
            column: "KeyId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_identity_api_keys_UserId",
            table: "identity_api_keys",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "identity_api_keys");
    }
}
