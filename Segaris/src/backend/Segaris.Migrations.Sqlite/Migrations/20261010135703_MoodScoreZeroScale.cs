using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class MoodScoreZeroScale : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Re-map existing entries from the 1-5 scale onto the 0-5 scale. A single
        // statement so every row is judged by its original score: 2 becomes 1, and
        // a 3 drops to 2 when its Alignment is Negative, or Medium with a Defensive
        // or Offensive Direction. Every other entry keeps its score.
        migrationBuilder.Sql(
            """
            UPDATE "mood_entries"
            SET "Score" = CASE
                WHEN "Score" = 2 THEN 1
                WHEN "Score" = 3 AND "Alignment" = 'Negative' THEN 2
                WHEN "Score" = 3 AND "Alignment" = 'Medium' AND "Direction" IN ('Defensive', 'Offensive') THEN 2
                ELSE "Score"
            END
            WHERE "Score" IN (2, 3);
            """);

        migrationBuilder.DropCheckConstraint(
            name: "CK_mood_entries_score",
            table: "mood_entries");

        migrationBuilder.AddCheckConstraint(
            name: "CK_mood_entries_score",
            table: "mood_entries",
            sql: "\"Score\" >= 0 AND \"Score\" <= 5");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The score re-mapping is not reversible; only lift zero scores back into
        // the old range so the restored constraint holds.
        migrationBuilder.Sql("UPDATE \"mood_entries\" SET \"Score\" = 1 WHERE \"Score\" = 0;");

        migrationBuilder.DropCheckConstraint(
            name: "CK_mood_entries_score",
            table: "mood_entries");

        migrationBuilder.AddCheckConstraint(
            name: "CK_mood_entries_score",
            table: "mood_entries",
            sql: "\"Score\" >= 1 AND \"Score\" <= 5");
    }
}
