using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class MoodIntentCriterion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intent replaces Direction and Source. The column starts nullable so the
        // existing entries can be converted before the old criteria are dropped.
        migrationBuilder.AddColumn<string>(
            name: "Intent",
            table: "mood_entries",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        // One WHEN per previous Energy/Alignment/Direction/Source combination (72),
        // taken verbatim from the product-supplied Mood v6 Migration.csv. Energy and
        // Alignment are kept; only the Intent is derived.
        migrationBuilder.Sql(
            """
            UPDATE "mood_entries"
            SET "Intent" = CASE
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Explore'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'High' AND "Alignment" = 'Positive' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Explore'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Attack'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Defend'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'High' AND "Alignment" = 'Medium' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Explore'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Attack'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Defend'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'High' AND "Alignment" = 'Negative' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Rebuild'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Attack'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Explore'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Positive' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Rebuild'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Defend'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Medium' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Explore'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Defend'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Explore'
                WHEN "Energy" = 'Medium' AND "Alignment" = 'Negative' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Explore'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Positive' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Attack'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Defend'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Medium' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Stay'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Harmony' AND "Source" = 'Internal' THEN 'Defend'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Harmony' AND "Source" = 'External' THEN 'Defend'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Offensive' AND "Source" = 'Internal' THEN 'Attack'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Offensive' AND "Source" = 'External' THEN 'Attack'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Defensive' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Defensive' AND "Source" = 'External' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Stability' AND "Source" = 'Internal' THEN 'Rebuild'
                WHEN "Energy" = 'Low' AND "Alignment" = 'Negative' AND "Direction" = 'Stability' AND "Source" = 'External' THEN 'Rebuild'
            END;
            """);

        migrationBuilder.DropCheckConstraint(
            name: "CK_mood_entries_direction",
            table: "mood_entries");

        migrationBuilder.DropCheckConstraint(
            name: "CK_mood_entries_source",
            table: "mood_entries");

        migrationBuilder.DropColumn(
            name: "Direction",
            table: "mood_entries");

        migrationBuilder.DropColumn(
            name: "Source",
            table: "mood_entries");

        migrationBuilder.AlterColumn<string>(
            name: "Intent",
            table: "mood_entries",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10,
            oldNullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_mood_entries_intent",
            table: "mood_entries",
            sql: "\"Intent\" IN ('Stay', 'Defend', 'Attack', 'Rebuild', 'Explore')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Direction",
            table: "mood_entries",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Source",
            table: "mood_entries",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        // The conversion is not reversible: the original Direction and Source are
        // gone. Restore the closest Direction and a fixed Source so the old
        // constraints hold.
        migrationBuilder.Sql(
            """
            UPDATE "mood_entries"
            SET "Direction" = CASE "Intent"
                    WHEN 'Defend' THEN 'Defensive'
                    WHEN 'Attack' THEN 'Offensive'
                    WHEN 'Rebuild' THEN 'Stability'
                    ELSE 'Harmony'
                END,
                "Source" = 'Internal';
            """);

        migrationBuilder.DropCheckConstraint(
            name: "CK_mood_entries_intent",
            table: "mood_entries");

        migrationBuilder.DropColumn(
            name: "Intent",
            table: "mood_entries");

        migrationBuilder.AlterColumn<string>(
            name: "Direction",
            table: "mood_entries",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Source",
            table: "mood_entries",
            type: "character varying(10)",
            maxLength: 10,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10,
            oldNullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_mood_entries_direction",
            table: "mood_entries",
            sql: "\"Direction\" IN ('Harmony', 'Defensive', 'Offensive', 'Stability')");

        migrationBuilder.AddCheckConstraint(
            name: "CK_mood_entries_source",
            table: "mood_entries",
            sql: "\"Source\" IN ('Internal', 'External')");
    }
}
