using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class CurrencyExchangeRateToEur : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "ExchangeRateToEur",
            table: "configuration_currencies",
            type: "TEXT",
            precision: 18,
            scale: 8,
            nullable: true);

        // Backfill the seeded currencies so upgraded databases match a fresh
        // install: EUR is fixed at 1 and the non-EUR seeds carry their
        // development placeholder rates. Any administrator-created currency keeps
        // a null rate until its rate is supplied through Configuration.
        migrationBuilder.Sql(
            "UPDATE \"configuration_currencies\" SET \"ExchangeRateToEur\" = 1 WHERE \"NormalizedCode\" = 'EUR';");
        migrationBuilder.Sql(
            "UPDATE \"configuration_currencies\" SET \"ExchangeRateToEur\" = 0.92 WHERE \"NormalizedCode\" = 'USD';");
        migrationBuilder.Sql(
            "UPDATE \"configuration_currencies\" SET \"ExchangeRateToEur\" = 1.17 WHERE \"NormalizedCode\" = 'GBP';");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ExchangeRateToEur",
            table: "configuration_currencies");
    }
}
