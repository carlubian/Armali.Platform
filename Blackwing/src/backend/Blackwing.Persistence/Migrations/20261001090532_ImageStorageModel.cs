using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Blackwing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImageStorageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ownership_probes");

            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SortedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, computedColumnSql: "COALESCE(\"CapturedAt\", \"UploadedAt\")", stored: true),
                    ReviewState = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "upload_jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DeclaredByteSize = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StagedFileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ImageId = table.Column<int>(type: "integer", nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upload_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_upload_jobs_images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "image_tags",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_image_tags", x => new { x.ImageId, x.TagId });
                    table.ForeignKey(
                        name: "FK_image_tags_images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_image_tags_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_image_tags_owner_tag_image",
                table: "image_tags",
                columns: new[] { "OwnerUserId", "TagId", "ImageId" });

            migrationBuilder.CreateIndex(
                name: "IX_image_tags_TagId",
                table: "image_tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_images_owner_content_hash",
                table: "images",
                columns: new[] { "OwnerUserId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_images_owner_review_state_sorted_at_id",
                table: "images",
                columns: new[] { "OwnerUserId", "ReviewState", "SortedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_images_owner_sorted_at_id",
                table: "images",
                columns: new[] { "OwnerUserId", "SortedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_tags_owner_kind_normalized_value",
                table: "tags",
                columns: new[] { "OwnerUserId", "Kind", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_upload_jobs_ImageId",
                table: "upload_jobs",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_upload_jobs_owner_batch_id",
                table: "upload_jobs",
                columns: new[] { "OwnerUserId", "BatchId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_upload_jobs_state_id",
                table: "upload_jobs",
                columns: new[] { "State", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "image_tags");

            migrationBuilder.DropTable(
                name: "upload_jobs");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "images");

            migrationBuilder.CreateTable(
                name: "ownership_probes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ownership_probes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ownership_probes_OwnerUserId",
                table: "ownership_probes",
                column: "OwnerUserId");
        }
    }
}
