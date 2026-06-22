using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class TravelDestinationReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destination",
                table: "travel_trips");

            migrationBuilder.AddColumn<int>(
                name: "DestinationId",
                table: "travel_trips",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_travel_trips_DestinationId",
                table: "travel_trips",
                column: "DestinationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_travel_trips_DestinationId",
                table: "travel_trips");

            migrationBuilder.DropColumn(
                name: "DestinationId",
                table: "travel_trips");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "travel_trips",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
