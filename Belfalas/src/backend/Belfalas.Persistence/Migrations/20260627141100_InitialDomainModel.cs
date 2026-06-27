using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belfalas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "world_templates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_world_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorldTemplateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_districts_world_templates_WorldTemplateId",
                        column: x => x.WorldTemplateId,
                        principalTable: "world_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Weeks = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    WorldTemplateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eras_world_templates_WorldTemplateId",
                        column: x => x.WorldTemplateId,
                        principalTable: "world_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorldTemplateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SpriteKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_variants_world_templates_WorldTemplateId",
                        column: x => x.WorldTemplateId,
                        principalTable: "world_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evolution_stages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DistrictId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    DenizenType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evolution_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evolution_stages_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DistrictId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PositionX = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionY = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plots_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "archived_eras",
                columns: table => new
                {
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Snapshot = table.Column<string>(type: "TEXT", maxLength: 2147483647, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archived_eras", x => x.EraId);
                    table.ForeignKey(
                        name: "FK_archived_eras_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DistrictId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_areas_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_areas_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "denizen_counts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DistrictId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DenizenType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_denizen_counts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_denizen_counts_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_denizen_counts_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_sets_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "built_plots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DistrictId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VariantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_built_plots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_built_plots_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_built_plots_plots_PlotId",
                        column: x => x.PlotId,
                        principalTable: "plots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_built_plots_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "area_progresses",
                columns: table => new
                {
                    AreaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Xp = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_area_progresses", x => x.AreaId);
                    table.ForeignKey(
                        name: "FK_area_progresses_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_area_progresses_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "daily_habits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AreaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Xp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_habits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_habits_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_daily_habits_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AreaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Xp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_goals_areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_weekly_goals_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "daily_completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DailyHabitId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_completions_daily_habits_DailyHabitId",
                        column: x => x.DailyHabitId,
                        principalTable: "daily_habits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_daily_completions_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_completions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EraId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    WeeklyGoalId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_completions_eras_EraId",
                        column: x => x.EraId,
                        principalTable: "eras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_weekly_completions_weekly_goals_WeeklyGoalId",
                        column: x => x.WeeklyGoalId,
                        principalTable: "weekly_goals",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "weekly_set_items",
                columns: table => new
                {
                    WeeklySetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeeklyGoalId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_set_items", x => new { x.WeeklySetId, x.WeeklyGoalId });
                    table.ForeignKey(
                        name: "FK_weekly_set_items_weekly_goals_WeeklyGoalId",
                        column: x => x.WeeklyGoalId,
                        principalTable: "weekly_goals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_weekly_set_items_weekly_sets_WeeklySetId",
                        column: x => x.WeeklySetId,
                        principalTable: "weekly_sets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_area_progresses_EraId",
                table: "area_progresses",
                column: "EraId");

            migrationBuilder.CreateIndex(
                name: "IX_areas_DistrictId",
                table: "areas",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_areas_EraId_Order",
                table: "areas",
                columns: new[] { "EraId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_built_plots_EraId_PlotId",
                table: "built_plots",
                columns: new[] { "EraId", "PlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_built_plots_PlotId",
                table: "built_plots",
                column: "PlotId");

            migrationBuilder.CreateIndex(
                name: "IX_built_plots_VariantId",
                table: "built_plots",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_completions_DailyHabitId",
                table: "daily_completions",
                column: "DailyHabitId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_completions_EraId_Date_DailyHabitId",
                table: "daily_completions",
                columns: new[] { "EraId", "Date", "DailyHabitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_habits_AreaId",
                table: "daily_habits",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_daily_habits_EraId",
                table: "daily_habits",
                column: "EraId");

            migrationBuilder.CreateIndex(
                name: "IX_denizen_counts_DistrictId",
                table: "denizen_counts",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_denizen_counts_EraId_DistrictId_DenizenType",
                table: "denizen_counts",
                columns: new[] { "EraId", "DistrictId", "DenizenType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_districts_WorldTemplateId_Slot",
                table: "districts",
                columns: new[] { "WorldTemplateId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_eras_WorldTemplateId",
                table: "eras",
                column: "WorldTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_evolution_stages_DistrictId_Order",
                table: "evolution_stages",
                columns: new[] { "DistrictId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plots_DistrictId_PositionX_PositionY",
                table: "plots",
                columns: new[] { "DistrictId", "PositionX", "PositionY" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_WorldTemplateId_Category",
                table: "variants",
                columns: new[] { "WorldTemplateId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_completions_EraId_WeekIndex_WeeklyGoalId",
                table: "weekly_completions",
                columns: new[] { "EraId", "WeekIndex", "WeeklyGoalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_completions_WeeklyGoalId",
                table: "weekly_completions",
                column: "WeeklyGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_goals_AreaId",
                table: "weekly_goals",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_goals_EraId",
                table: "weekly_goals",
                column: "EraId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_set_items_WeeklyGoalId",
                table: "weekly_set_items",
                column: "WeeklyGoalId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_sets_EraId_WeekIndex",
                table: "weekly_sets",
                columns: new[] { "EraId", "WeekIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "archived_eras");

            migrationBuilder.DropTable(
                name: "area_progresses");

            migrationBuilder.DropTable(
                name: "built_plots");

            migrationBuilder.DropTable(
                name: "daily_completions");

            migrationBuilder.DropTable(
                name: "denizen_counts");

            migrationBuilder.DropTable(
                name: "evolution_stages");

            migrationBuilder.DropTable(
                name: "weekly_completions");

            migrationBuilder.DropTable(
                name: "weekly_set_items");

            migrationBuilder.DropTable(
                name: "plots");

            migrationBuilder.DropTable(
                name: "variants");

            migrationBuilder.DropTable(
                name: "daily_habits");

            migrationBuilder.DropTable(
                name: "weekly_goals");

            migrationBuilder.DropTable(
                name: "weekly_sets");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "eras");

            migrationBuilder.DropTable(
                name: "world_templates");
        }
    }
}
