using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belfalas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldTemplateRenderingContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetBasePath",
                table: "world_templates",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AtlasKey",
                table: "world_templates",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CameraMaxX",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CameraMaxY",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CameraMinX",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CameraMinY",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MapHeight",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MapWidth",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginX",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginY",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TileHeight",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TileWidth",
                table: "world_templates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "category_contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorldTemplateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FootprintWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    FootprintHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    AnchorX = table.Column<double>(type: "REAL", nullable: false),
                    AnchorY = table.Column<double>(type: "REAL", nullable: false),
                    SortOffsetY = table.Column<int>(type: "INTEGER", nullable: false),
                    SupportsDenizens = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_category_contracts_world_templates_WorldTemplateId",
                        column: x => x.WorldTemplateId,
                        principalTable: "world_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "denizen_sockets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DistrictId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PositionX = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionY = table.Column<int>(type: "INTEGER", nullable: false),
                    AnchorX = table.Column<double>(type: "REAL", nullable: false),
                    AnchorY = table.Column<double>(type: "REAL", nullable: false),
                    SortOffsetY = table.Column<int>(type: "INTEGER", nullable: false),
                    CompatibleDenizenTypes = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_denizen_sockets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_denizen_sockets_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_contracts_WorldTemplateId_Category",
                table: "category_contracts",
                columns: new[] { "WorldTemplateId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_denizen_sockets_DistrictId_PositionX_PositionY",
                table: "denizen_sockets",
                columns: new[] { "DistrictId", "PositionX", "PositionY" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_contracts");

            migrationBuilder.DropTable(
                name: "denizen_sockets");

            migrationBuilder.DropColumn(
                name: "AssetBasePath",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "AtlasKey",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "CameraMaxX",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "CameraMaxY",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "CameraMinX",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "CameraMinY",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "MapHeight",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "MapWidth",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "OriginX",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "OriginY",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "TileHeight",
                table: "world_templates");

            migrationBuilder.DropColumn(
                name: "TileWidth",
                table: "world_templates");
        }
    }
}
