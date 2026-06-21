using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class FirebirdDomainPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "firebird_person_categories",
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
                    table.PrimaryKey("PK_firebird_person_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "firebird_username_platforms",
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
                    table.PrimaryKey("PK_firebird_username_platforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "firebird_people",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BirthdayMonth = table.Column<int>(type: "integer", nullable: true),
                    BirthdayDay = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AvatarAttachmentId = table.Column<int>(type: "integer", nullable: true),
                    Visibility = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_firebird_people", x => x.Id);
                    table.CheckConstraint("CK_firebird_people_birthday", "(\"BirthdayMonth\" IS NULL AND \"BirthdayDay\" IS NULL) OR (\"BirthdayMonth\" BETWEEN 1 AND 12 AND \"BirthdayDay\" BETWEEN 1 AND CASE WHEN \"BirthdayMonth\" = 2 THEN 29 WHEN \"BirthdayMonth\" IN (4, 6, 9, 11) THEN 30 ELSE 31 END)");
                    table.CheckConstraint("CK_firebird_people_status", "\"Status\" IN ('Unknown', 'Active', 'Unavailable', 'Blocked')");
                    table.CheckConstraint("CK_firebird_people_visibility", "\"Visibility\" IN ('Public', 'Private')");
                    table.ForeignKey(
                        name: "FK_firebird_people_firebird_person_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "firebird_person_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_firebird_people_identity_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "identity_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_firebird_people_identity_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "identity_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "firebird_interactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_firebird_interactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_firebird_interactions_firebird_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "firebird_people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_firebird_interactions_identity_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "identity_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_firebird_interactions_identity_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "identity_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "firebird_usernames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    PlatformId = table.Column<int>(type: "integer", nullable: false),
                    Handle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_firebird_usernames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_firebird_usernames_firebird_people_PersonId",
                        column: x => x.PersonId,
                        principalTable: "firebird_people",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_firebird_usernames_firebird_username_platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "firebird_username_platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_firebird_usernames_identity_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "identity_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_firebird_usernames_identity_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "identity_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_interactions_CreatedBy",
                table: "firebird_interactions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_interactions_PersonId_Date_Id",
                table: "firebird_interactions",
                columns: new[] { "PersonId", "Date", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_interactions_UpdatedBy",
                table: "firebird_interactions",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_BirthdayMonth_BirthdayDay_Id",
                table: "firebird_people",
                columns: new[] { "BirthdayMonth", "BirthdayDay", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_CategoryId",
                table: "firebird_people",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_CreatedBy_Visibility_Id",
                table: "firebird_people",
                columns: new[] { "CreatedBy", "Visibility", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_Name_Id",
                table: "firebird_people",
                columns: new[] { "Name", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_Status",
                table: "firebird_people",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_UpdatedBy",
                table: "firebird_people",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_Visibility",
                table: "firebird_people",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_people_Visibility_BirthdayMonth_BirthdayDay",
                table: "firebird_people",
                columns: new[] { "Visibility", "BirthdayMonth", "BirthdayDay" });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_person_categories_NormalizedName",
                table: "firebird_person_categories",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_firebird_person_categories_SortOrder",
                table: "firebird_person_categories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_username_platforms_NormalizedName",
                table: "firebird_username_platforms",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_firebird_username_platforms_SortOrder",
                table: "firebird_username_platforms",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_usernames_CreatedBy",
                table: "firebird_usernames",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_usernames_PersonId_Id",
                table: "firebird_usernames",
                columns: new[] { "PersonId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_firebird_usernames_PlatformId",
                table: "firebird_usernames",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_firebird_usernames_UpdatedBy",
                table: "firebird_usernames",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "firebird_interactions");

            migrationBuilder.DropTable(
                name: "firebird_usernames");

            migrationBuilder.DropTable(
                name: "firebird_people");

            migrationBuilder.DropTable(
                name: "firebird_username_platforms");

            migrationBuilder.DropTable(
                name: "firebird_person_categories");
        }
    }
}
