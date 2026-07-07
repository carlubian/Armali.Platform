using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class GamesDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "games_games",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games_games", x => x.Id);
                table.CheckConstraint("CK_games_games_platform", "\"Platform\" IN ('PC', 'Console', 'Mobile', 'BoardGame', 'TabletopRpg', 'Other')");
                table.ForeignKey(
                    name: "FK_games_games_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_games_games_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "games_playthroughs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                GameId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                StartYear = table.Column<int>(type: "integer", nullable: false),
                StartMonth = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Visibility = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games_playthroughs", x => x.Id);
                table.CheckConstraint("CK_games_playthroughs_start_month", "\"StartMonth\" BETWEEN 1 AND 12");
                table.CheckConstraint("CK_games_playthroughs_start_year", "\"StartYear\" BETWEEN 1 AND 9999");
                table.CheckConstraint("CK_games_playthroughs_status", "\"Status\" IN ('Planning', 'Active', 'Completed')");
                table.CheckConstraint("CK_games_playthroughs_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_games_playthroughs_games_games_GameId",
                    column: x => x.GameId,
                    principalTable: "games_games",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_games_playthroughs_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_games_playthroughs_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "games_playthrough_tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PlaythroughId = table.Column<int>(type: "integer", nullable: false),
                Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                NormalizedValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games_playthrough_tags", x => x.Id);
                table.ForeignKey(
                    name: "FK_games_playthrough_tags_games_playthroughs_PlaythroughId",
                    column: x => x.PlaythroughId,
                    principalTable: "games_playthroughs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "games_sections",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PlaythroughId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games_sections", x => x.Id);
                table.CheckConstraint("CK_games_sections_color", "\"Color\" IN ('Blue', 'Green', 'Amber', 'Red', 'Purple', 'Pink', 'Teal', 'Indigo', 'Slate', 'Orange')");
                table.ForeignKey(
                    name: "FK_games_sections_games_playthroughs_PlaythroughId",
                    column: x => x.PlaythroughId,
                    principalTable: "games_playthroughs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_games_sections_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_games_sections_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "games_goals",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SectionId = table.Column<int>(type: "integer", nullable: false),
                Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Completed = table.Column<bool>(type: "boolean", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games_goals", x => x.Id);
                table.ForeignKey(
                    name: "FK_games_goals_games_sections_SectionId",
                    column: x => x.SectionId,
                    principalTable: "games_sections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_games_goals_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_games_goals_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_games_games_CreatedBy",
            table: "games_games",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_games_games_NormalizedName",
            table: "games_games",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_games_games_SortOrder_Id",
            table: "games_games",
            columns: new[] { "SortOrder", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_games_UpdatedBy",
            table: "games_games",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_games_goals_Completed",
            table: "games_goals",
            column: "Completed");

        migrationBuilder.CreateIndex(
            name: "IX_games_goals_CreatedBy",
            table: "games_goals",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_games_goals_SectionId_Position_Id",
            table: "games_goals",
            columns: new[] { "SectionId", "Position", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_goals_UpdatedBy",
            table: "games_goals",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_games_playthrough_tags_NormalizedValue_PlaythroughId",
            table: "games_playthrough_tags",
            columns: new[] { "NormalizedValue", "PlaythroughId" });

        migrationBuilder.CreateIndex(
            name: "IX_games_playthrough_tags_PlaythroughId_NormalizedValue",
            table: "games_playthrough_tags",
            columns: new[] { "PlaythroughId", "NormalizedValue" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_games_playthrough_tags_PlaythroughId_SortOrder_Id",
            table: "games_playthrough_tags",
            columns: new[] { "PlaythroughId", "SortOrder", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_CreatedBy_Visibility_Id",
            table: "games_playthroughs",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_GameId",
            table: "games_playthroughs",
            column: "GameId");

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_NormalizedName_Id",
            table: "games_playthroughs",
            columns: new[] { "NormalizedName", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_StartYear_StartMonth_Id",
            table: "games_playthroughs",
            columns: new[] { "StartYear", "StartMonth", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_Status_Id",
            table: "games_playthroughs",
            columns: new[] { "Status", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_UpdatedBy",
            table: "games_playthroughs",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_games_playthroughs_Visibility",
            table: "games_playthroughs",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_games_sections_CreatedBy",
            table: "games_sections",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_games_sections_PlaythroughId_NormalizedName",
            table: "games_sections",
            columns: new[] { "PlaythroughId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_games_sections_PlaythroughId_SortOrder_Id",
            table: "games_sections",
            columns: new[] { "PlaythroughId", "SortOrder", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_games_sections_UpdatedBy",
            table: "games_sections",
            column: "UpdatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "games_goals");

        migrationBuilder.DropTable(
            name: "games_playthrough_tags");

        migrationBuilder.DropTable(
            name: "games_sections");

        migrationBuilder.DropTable(
            name: "games_playthroughs");

        migrationBuilder.DropTable(
            name: "games_games");
    }
}
