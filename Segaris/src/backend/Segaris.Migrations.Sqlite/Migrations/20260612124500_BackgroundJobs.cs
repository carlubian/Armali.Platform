using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class BackgroundJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "platform_background_jobs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                JobType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                State = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ActiveExclusivityKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                Parameters = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                Progress = table.Column<int>(type: "INTEGER", nullable: true),
                ProgressCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                ResultReference = table.Column<string>(type: "TEXT", maxLength: 260, nullable: true),
                ResultCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                FailureCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                TraceId = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                CancellationRequested = table.Column<bool>(type: "INTEGER", nullable: false),
                CancellationRequestedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_platform_background_jobs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_platform_background_jobs_ActiveExclusivityKey",
            table: "platform_background_jobs",
            column: "ActiveExclusivityKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_platform_background_jobs_State",
            table: "platform_background_jobs",
            column: "State");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "platform_background_jobs");
    }
}
