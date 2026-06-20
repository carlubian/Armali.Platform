using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Segaris.Migrations.Sqlite.Migrations;

/// <inheritdoc />
public partial class ProjectsDomainPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "projects_number_allocations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AllocatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects_number_allocations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "projects_programs",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects_programs", x => x.Id);
                table.CheckConstraint("CK_projects_programs_code", "length(\"Code\") = 4 AND \"Code\" = upper(\"Code\")");
                table.ForeignKey(
                    name: "FK_projects_programs_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_programs_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "projects_axes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects_axes", x => x.Id);
                table.CheckConstraint("CK_projects_axes_code", "length(\"Code\") = 4 AND \"Code\" = upper(\"Code\")");
                table.ForeignKey(
                    name: "FK_projects_axes_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_axes_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_axes_projects_programs_ProgramId",
                    column: x => x.ProgramId,
                    principalTable: "projects_programs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "projects_activities",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AxisId = table.Column<int>(type: "INTEGER", nullable: false),
                Number = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects_activities", x => x.Id);
                table.CheckConstraint("CK_projects_activities_number", "\"Number\" > 0");
                table.CheckConstraint("CK_projects_activities_status", "\"Status\" IN ('Planning', 'Active', 'Completed', 'OnHold', 'Cancelled')");
                table.CheckConstraint("CK_projects_activities_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_projects_activities_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_activities_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_activities_projects_axes_AxisId",
                    column: x => x.AxisId,
                    principalTable: "projects_axes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "projects_projects",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AxisId = table.Column<int>(type: "INTEGER", nullable: false),
                Number = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Visibility = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedBy = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects_projects", x => x.Id);
                table.CheckConstraint("CK_projects_projects_number", "\"Number\" > 0");
                table.CheckConstraint("CK_projects_projects_status", "\"Status\" IN ('Planning', 'Active', 'Completed', 'OnHold', 'Cancelled')");
                table.CheckConstraint("CK_projects_projects_visibility", "\"Visibility\" IN ('Public', 'Private')");
                table.ForeignKey(
                    name: "FK_projects_projects_identity_users_CreatedBy",
                    column: x => x.CreatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_projects_identity_users_UpdatedBy",
                    column: x => x.UpdatedBy,
                    principalTable: "identity_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_projects_projects_projects_axes_AxisId",
                    column: x => x.AxisId,
                    principalTable: "projects_axes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_projects_activities_AxisId_Number_Id",
            table: "projects_activities",
            columns: new[] { "AxisId", "Number", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_activities_CreatedBy_Visibility_Id",
            table: "projects_activities",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_activities_Number",
            table: "projects_activities",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_projects_activities_UpdatedBy",
            table: "projects_activities",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_activities_Visibility",
            table: "projects_activities",
            column: "Visibility");

        migrationBuilder.CreateIndex(
            name: "IX_projects_axes_Code",
            table: "projects_axes",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_projects_axes_CreatedBy",
            table: "projects_axes",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_axes_ProgramId_Code_Id",
            table: "projects_axes",
            columns: new[] { "ProgramId", "Code", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_axes_UpdatedBy",
            table: "projects_axes",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_programs_Code",
            table: "projects_programs",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_projects_programs_Code_Id",
            table: "projects_programs",
            columns: new[] { "Code", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_programs_CreatedBy",
            table: "projects_programs",
            column: "CreatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_programs_UpdatedBy",
            table: "projects_programs",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_projects_AxisId_Number_Id",
            table: "projects_projects",
            columns: new[] { "AxisId", "Number", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_projects_CreatedBy_Visibility_Id",
            table: "projects_projects",
            columns: new[] { "CreatedBy", "Visibility", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_projects_projects_Number",
            table: "projects_projects",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_projects_projects_UpdatedBy",
            table: "projects_projects",
            column: "UpdatedBy");

        migrationBuilder.CreateIndex(
            name: "IX_projects_projects_Visibility",
            table: "projects_projects",
            column: "Visibility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "projects_activities");

        migrationBuilder.DropTable(
            name: "projects_number_allocations");

        migrationBuilder.DropTable(
            name: "projects_projects");

        migrationBuilder.DropTable(
            name: "projects_axes");

        migrationBuilder.DropTable(
            name: "projects_programs");
    }
}
