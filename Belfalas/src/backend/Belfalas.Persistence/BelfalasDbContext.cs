using Belfalas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Belfalas.Persistence;

public sealed class BelfalasDbContext(DbContextOptions<BelfalasDbContext> options) : DbContext(options)
{
    // Era configuration
    public DbSet<Era> Eras => Set<Era>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<DailyHabit> DailyHabits => Set<DailyHabit>();
    public DbSet<WeeklyGoal> WeeklyGoals => Set<WeeklyGoal>();

    // World template (reference data)
    public DbSet<WorldTemplate> WorldTemplates => Set<WorldTemplate>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Plot> Plots => Set<Plot>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<EvolutionStage> EvolutionStages => Set<EvolutionStage>();

    // Era runtime state
    public DbSet<AreaProgress> AreaProgresses => Set<AreaProgress>();
    public DbSet<WeeklySet> WeeklySets => Set<WeeklySet>();
    public DbSet<WeeklySetItem> WeeklySetItems => Set<WeeklySetItem>();
    public DbSet<DailyCompletion> DailyCompletions => Set<DailyCompletion>();
    public DbSet<WeeklyCompletion> WeeklyCompletions => Set<WeeklyCompletion>();
    public DbSet<BuiltPlot> BuiltPlots => Set<BuiltPlot>();
    public DbSet<DenizenCount> DenizenCounts => Set<DenizenCount>();
    public DbSet<ArchivedEra> ArchivedEras => Set<ArchivedEra>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveMaxLength(200);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureEra(modelBuilder.Entity<Era>());
        ConfigureArea(modelBuilder.Entity<Area>());
        ConfigureDailyHabit(modelBuilder.Entity<DailyHabit>());
        ConfigureWeeklyGoal(modelBuilder.Entity<WeeklyGoal>());

        ConfigureWorldTemplate(modelBuilder.Entity<WorldTemplate>());
        ConfigureDistrict(modelBuilder.Entity<District>());
        ConfigurePlot(modelBuilder.Entity<Plot>());
        ConfigureVariant(modelBuilder.Entity<Variant>());
        ConfigureEvolutionStage(modelBuilder.Entity<EvolutionStage>());

        ConfigureAreaProgress(modelBuilder.Entity<AreaProgress>());
        ConfigureWeeklySet(modelBuilder.Entity<WeeklySet>());
        ConfigureWeeklySetItem(modelBuilder.Entity<WeeklySetItem>());
        ConfigureDailyCompletion(modelBuilder.Entity<DailyCompletion>());
        ConfigureWeeklyCompletion(modelBuilder.Entity<WeeklyCompletion>());
        ConfigureBuiltPlot(modelBuilder.Entity<BuiltPlot>());
        ConfigureDenizenCount(modelBuilder.Entity<DenizenCount>());
        ConfigureArchivedEra(modelBuilder.Entity<ArchivedEra>());
    }

    private static void ConfigureEra(EntityTypeBuilder<Era> builder)
    {
        builder.ToTable("eras");
        builder.HasKey(era => era.Id);
        builder.Property(era => era.Name).IsRequired();
        builder.Property(era => era.WorldTemplateId).HasMaxLength(64).IsRequired();
        builder.Property(era => era.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(era => era.XpPerLevel).HasDefaultValue(100);

        builder.HasOne(era => era.WorldTemplate)
            .WithMany()
            .HasForeignKey(era => era.WorldTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureArea(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("areas");
        builder.HasKey(area => area.Id);
        builder.Property(area => area.Name).IsRequired();

        builder.HasOne(area => area.Era)
            .WithMany(era => era.Areas)
            .HasForeignKey(area => area.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(area => area.District)
            .WithMany()
            .HasForeignKey(area => area.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(area => new { area.EraId, area.Order });
    }

    private static void ConfigureDailyHabit(EntityTypeBuilder<DailyHabit> builder)
    {
        builder.ToTable("daily_habits");
        builder.HasKey(habit => habit.Id);
        builder.Property(habit => habit.Label).IsRequired();

        builder.HasOne(habit => habit.Era)
            .WithMany(era => era.DailyHabits)
            .HasForeignKey(habit => habit.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(habit => habit.Area)
            .WithMany()
            .HasForeignKey(habit => habit.AreaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(habit => habit.EraId);
    }

    private static void ConfigureWeeklyGoal(EntityTypeBuilder<WeeklyGoal> builder)
    {
        builder.ToTable("weekly_goals");
        builder.HasKey(goal => goal.Id);
        builder.Property(goal => goal.Label).IsRequired();

        builder.HasOne(goal => goal.Era)
            .WithMany(era => era.WeeklyGoals)
            .HasForeignKey(goal => goal.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(goal => goal.Area)
            .WithMany()
            .HasForeignKey(goal => goal.AreaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(goal => goal.EraId);
    }

    private static void ConfigureWorldTemplate(EntityTypeBuilder<WorldTemplate> builder)
    {
        builder.ToTable("world_templates");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).HasMaxLength(64);
        builder.Property(template => template.Theme).IsRequired();
        builder.Property(template => template.Name).IsRequired();
    }

    private static void ConfigureDistrict(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts");
        builder.HasKey(district => district.Id);
        builder.Property(district => district.WorldTemplateId).HasMaxLength(64).IsRequired();
        builder.Property(district => district.Name).IsRequired();

        builder.HasOne(district => district.WorldTemplate)
            .WithMany(template => template.Districts)
            .HasForeignKey(district => district.WorldTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(district => new { district.WorldTemplateId, district.Slot }).IsUnique();
    }

    private static void ConfigurePlot(EntityTypeBuilder<Plot> builder)
    {
        builder.ToTable("plots");
        builder.HasKey(plot => plot.Id);
        builder.Property(plot => plot.Category).HasMaxLength(64).IsRequired();

        builder.HasOne(plot => plot.District)
            .WithMany(district => district.Plots)
            .HasForeignKey(plot => plot.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(plot => new { plot.DistrictId, plot.PositionX, plot.PositionY }).IsUnique();
    }

    private static void ConfigureVariant(EntityTypeBuilder<Variant> builder)
    {
        builder.ToTable("variants");
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.WorldTemplateId).HasMaxLength(64).IsRequired();
        builder.Property(variant => variant.Category).HasMaxLength(64).IsRequired();
        builder.Property(variant => variant.SpriteKey).HasMaxLength(128).IsRequired();

        builder.HasOne(variant => variant.WorldTemplate)
            .WithMany(template => template.Variants)
            .HasForeignKey(variant => variant.WorldTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(variant => new { variant.WorldTemplateId, variant.Category });
    }

    private static void ConfigureEvolutionStage(EntityTypeBuilder<EvolutionStage> builder)
    {
        builder.ToTable("evolution_stages");
        builder.HasKey(stage => stage.Id);
        builder.Property(stage => stage.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(stage => stage.DenizenType).HasMaxLength(64);

        builder.HasOne(stage => stage.District)
            .WithMany(district => district.EvolutionStages)
            .HasForeignKey(stage => stage.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(stage => new { stage.DistrictId, stage.Order }).IsUnique();
    }

    private static void ConfigureAreaProgress(EntityTypeBuilder<AreaProgress> builder)
    {
        builder.ToTable("area_progresses");
        builder.HasKey(progress => progress.AreaId);

        builder.HasOne(progress => progress.Area)
            .WithMany()
            .HasForeignKey(progress => progress.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(progress => progress.Era)
            .WithMany()
            .HasForeignKey(progress => progress.EraId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(progress => progress.EraId);
    }

    private static void ConfigureWeeklySet(EntityTypeBuilder<WeeklySet> builder)
    {
        builder.ToTable("weekly_sets");
        builder.HasKey(set => set.Id);

        builder.HasOne(set => set.Era)
            .WithMany()
            .HasForeignKey(set => set.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(set => new { set.EraId, set.WeekIndex }).IsUnique();
    }

    private static void ConfigureWeeklySetItem(EntityTypeBuilder<WeeklySetItem> builder)
    {
        builder.ToTable("weekly_set_items");
        builder.HasKey(item => new { item.WeeklySetId, item.WeeklyGoalId });

        builder.HasOne(item => item.WeeklySet)
            .WithMany(set => set.Items)
            .HasForeignKey(item => item.WeeklySetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.WeeklyGoal)
            .WithMany()
            .HasForeignKey(item => item.WeeklyGoalId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureDailyCompletion(EntityTypeBuilder<DailyCompletion> builder)
    {
        builder.ToTable("daily_completions");
        builder.HasKey(completion => completion.Id);

        builder.HasOne(completion => completion.Era)
            .WithMany()
            .HasForeignKey(completion => completion.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(completion => completion.DailyHabit)
            .WithMany()
            .HasForeignKey(completion => completion.DailyHabitId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(completion => new { completion.EraId, completion.Date, completion.DailyHabitId }).IsUnique();
    }

    private static void ConfigureWeeklyCompletion(EntityTypeBuilder<WeeklyCompletion> builder)
    {
        builder.ToTable("weekly_completions");
        builder.HasKey(completion => completion.Id);

        builder.HasOne(completion => completion.Era)
            .WithMany()
            .HasForeignKey(completion => completion.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(completion => completion.WeeklyGoal)
            .WithMany()
            .HasForeignKey(completion => completion.WeeklyGoalId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(completion => new { completion.EraId, completion.WeekIndex, completion.WeeklyGoalId }).IsUnique();
    }

    private static void ConfigureBuiltPlot(EntityTypeBuilder<BuiltPlot> builder)
    {
        builder.ToTable("built_plots");
        builder.HasKey(plot => plot.Id);

        builder.HasOne(plot => plot.Era)
            .WithMany()
            .HasForeignKey(plot => plot.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(plot => plot.Plot)
            .WithMany()
            .HasForeignKey(plot => plot.PlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(plot => plot.Variant)
            .WithMany()
            .HasForeignKey(plot => plot.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(plot => new { plot.EraId, plot.PlotId }).IsUnique();
    }

    private static void ConfigureDenizenCount(EntityTypeBuilder<DenizenCount> builder)
    {
        builder.ToTable("denizen_counts");
        builder.HasKey(denizen => denizen.Id);
        builder.Property(denizen => denizen.DenizenType).HasMaxLength(64).IsRequired();

        builder.HasOne(denizen => denizen.Era)
            .WithMany()
            .HasForeignKey(denizen => denizen.EraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(denizen => denizen.District)
            .WithMany()
            .HasForeignKey(denizen => denizen.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(denizen => new { denizen.EraId, denizen.DistrictId, denizen.DenizenType }).IsUnique();
    }

    private static void ConfigureArchivedEra(EntityTypeBuilder<ArchivedEra> builder)
    {
        builder.ToTable("archived_eras");
        builder.HasKey(archive => archive.EraId);
        builder.Property(archive => archive.Snapshot).HasMaxLength(int.MaxValue).IsRequired();

        builder.HasOne(archive => archive.Era)
            .WithMany()
            .HasForeignKey(archive => archive.EraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
