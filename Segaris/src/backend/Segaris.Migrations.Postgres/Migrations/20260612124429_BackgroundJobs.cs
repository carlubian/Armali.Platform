using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Segaris.Migrations.Postgres.Migrations;

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
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                JobType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                State = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                ActiveExclusivityKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                Parameters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                Progress = table.Column<int>(type: "integer", nullable: true),
                ProgressCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                ResultReference = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                ResultCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                FailureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                TraceId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                CreatedBy = table.Column<int>(type: "integer", nullable: true),
                CancellationRequested = table.Column<bool>(type: "boolean", nullable: false),
                CancellationRequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
