using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blackwing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GalleryEffectiveDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_images_OwnerUserId_CapturedAt_Id",
                table: "images");

            migrationBuilder.DropIndex(
                name: "IX_images_OwnerUserId_UploadedAt_Id",
                table: "images");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveCapturedAt",
                table: "images",
                type: "timestamp with time zone",
                nullable: false,
                computedColumnSql: "COALESCE(\"CapturedAt\", \"UploadedAt\")",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_images_OwnerUserId_EffectiveCapturedAt_Id",
                table: "images",
                columns: new[] { "OwnerUserId", "EffectiveCapturedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_images_OwnerUserId_EffectiveCapturedAt_Id",
                table: "images");

            migrationBuilder.DropColumn(
                name: "EffectiveCapturedAt",
                table: "images");

            migrationBuilder.CreateIndex(
                name: "IX_images_OwnerUserId_CapturedAt_Id",
                table: "images",
                columns: new[] { "OwnerUserId", "CapturedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_images_OwnerUserId_UploadedAt_Id",
                table: "images",
                columns: new[] { "OwnerUserId", "UploadedAt", "Id" });
        }
    }
}
