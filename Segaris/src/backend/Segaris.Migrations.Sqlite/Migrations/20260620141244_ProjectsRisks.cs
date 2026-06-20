using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class ProjectsRisks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "projects_risks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Probability = table.Column<int>(type: "INTEGER", nullable: false),
                Impact = table.Column<int>(type: "INTEGER", nullable: false),
                Mitigation = table.Column<int>(type: "INTEGER", nullable: false),
                Score = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects_risks", x => x.Id);
                table.CheckConstraint("CK_projects_risks_impact", "\"Impact\" BETWEEN 1 AND 5");
                table.CheckConstraint("CK_projects_risks_mitigation", "\"Mitigation\" BETWEEN 1 AND 5");
                table.CheckConstraint("CK_projects_risks_probability", "\"Probability\" BETWEEN 1 AND 5");
                table.CheckConstraint("CK_projects_risks_score", "\"Score\" BETWEEN 1 AND 125");
                table.ForeignKey(
                    name: "FK_projects_risks_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_risks_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_risks_projects_projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "projects_projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_projects_risks_CreatedBy",
            table: "projects_risks",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_risks_ProjectId_Id",
            table: "projects_risks",
            columns: new[] { "ProjectId", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_risks_Score",
            table: "projects_risks",
            column: "Score");

        migrationBuilder.CreateIndex(
            name: "IX_projects_risks_UpdatedBy",
            table: "projects_risks",
            column: "UpdatedBy");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "projects_risks");
    }
}
