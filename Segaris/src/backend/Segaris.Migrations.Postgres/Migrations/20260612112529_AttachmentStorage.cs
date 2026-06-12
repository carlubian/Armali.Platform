using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class AttachmentStorage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "platform_attachments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EntityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                StorageFileName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_platform_attachments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_platform_attachments_Module_EntityType_EntityId",
            table: "platform_attachments",
            columns: new[] { "Module", "EntityType", "EntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_platform_attachments_StorageFileName",
            table: "platform_attachments",
            column: "StorageFileName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "platform_attachments");
    }
}
