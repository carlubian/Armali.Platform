using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class IdentityProfile : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DisplayName",
            table: "identity_users",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Language",
            table: "identity_users",
            type: "TEXT",
            maxLength: 10,
            nullable: false,
            defaultValue: "en-GB");

        migrationBuilder.Sql(
            "UPDATE identity_users SET DisplayName = UserName WHERE DisplayName = '';");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DisplayName",
            table: "identity_users");

        migrationBuilder.DropColumn(
            name: "Language",
            table: "identity_users");
    }
}
