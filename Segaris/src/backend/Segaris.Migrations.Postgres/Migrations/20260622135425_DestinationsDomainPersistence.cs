using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class DestinationsDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "destination_categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_destination_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "place_categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_place_categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "destinations",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "integer", nullable: false),
                Country = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                EntryRequirements = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IsSchengenArea = table.Column<bool>(type: "boolean", nullable: false),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Visibility = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                PrimaryAttachmentId = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_destinations", x => x.Id);
                table.CheckConstraint("CK_destinations_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_destinations_destination_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "destination_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_destinations_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_destinations_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "destination_places",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DestinationId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CategoryId = table.Column<int>(type: "integer", nullable: false),
                Rating = table.Column<int>(type: "integer", nullable: true),
                Review = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_destination_places", x => x.Id);
                table.CheckConstraint("CK_destination_places_rating", "\"Rating\" IS NULL OR \"Rating\" BETWEEN 1 AND 5");
                table.ForeignKey(
                    name: "FK_destination_places_destinations_DestinationId",
                    column: x => x.DestinationId,
                    principalTable: "destinations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_destination_places_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_destination_places_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_destination_places_place_categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "place_categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_destination_categories_NormalizedName",
            table: "destination_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_destination_categories_SortOrder",
            table: "destination_categories",
            column: "SortOrder");

        migrationBuilder.CreateIndex(
            name: "IX_destination_places_CategoryId",
            table: "destination_places",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_destination_places_CreatedBy",
            table: "destination_places",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_destination_places_DestinationId_CategoryId",
            table: "destination_places",
            columns: new[] { "DestinationId", "CategoryId" });

        migrationBuilder.CreateIndex(
            name: "IX_destination_places_DestinationId_Name_Id",
            table: "destination_places",
            columns: new[] { "DestinationId", "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_destination_places_DestinationId_Rating",
            table: "destination_places",
            columns: new[] { "DestinationId", "Rating" });

        migrationBuilder.CreateIndex(
            name: "IX_destination_places_UpdatedBy",
            table: "destination_places",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_destinations_CategoryId",
            table: "destinations",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_destinations_CreatedBy_Visibility_Id",
            table: "destinations",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_destinations_IsSchengenArea",
            table: "destinations",
            column: "IsSchengenArea");

        migrationBuilder.CreateIndex(
            name: "IX_destinations_Name_Id",
            table: "destinations",
            columns: new[] { "Name", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_destinations_UpdatedBy",
            table: "destinations",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_destinations_Visibility",
            table: "destinations",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_place_categories_NormalizedName",
            table: "place_categories",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_place_categories_SortOrder",
            table: "place_categories",
            column: "SortOrder");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "destination_places");

        migrationBuilder.DropTable(
            name: "destinations");

        migrationBuilder.DropTable(
            name: "place_categories");

        migrationBuilder.DropTable(
            name: "destination_categories");
    }
}
