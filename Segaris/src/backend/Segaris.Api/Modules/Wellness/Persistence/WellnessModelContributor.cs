using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Wellness.Persistence;

internal sealed class WellnessModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WellnessTask>(builder =>
        {
            builder.ToTable("wellness_tasks");
            builder.HasKey(task => task.Id);
            builder.Property(task => task.Id).ValueGeneratedOnAdd();
            builder.Property(task => task.Name).HasMaxLength(WellnessDefaults.TaskNameMaximumLength).IsRequired();
            builder.Property(task => task.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(task => task.SortOrder).IsRequired();
            builder.Property(task => task.CreatedAt).IsRequired();
            builder.Property(task => task.CreatedBy);
            builder.Property(task => task.UpdatedAt).IsRequired();
            builder.Property(task => task.UpdatedBy);
            builder.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_wellness_tasks_category",
                    "\"Category\" IN ('HealthAndBody', 'MindAndSleep', 'PeopleAndWork')"));
            builder.HasIndex(task => new { task.SortOrder, task.Id });
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(task => task.CreatedBy).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(task => task.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WellnessDay>(builder =>
        {
            builder.ToTable("wellness_days");
            builder.HasKey(day => day.Id);
            builder.Property(day => day.Id).ValueGeneratedOnAdd();
            builder.Property(day => day.Date).IsRequired();
            builder.Property(day => day.Score);
            builder.Property(day => day.CreatedAt).IsRequired();
            builder.Property(day => day.CreatedBy).IsRequired();
            builder.Property(day => day.UpdatedAt).IsRequired();
            builder.Property(day => day.UpdatedBy).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_wellness_days_score",
                    "\"Score\" IS NULL OR (\"Score\" BETWEEN 0 AND 100)"));
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(day => day.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(day => day.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(day => new { day.CreatedBy, day.Date }).IsUnique();
            builder.HasIndex(day => new { day.CreatedBy, day.Date, day.Id });
        });

        modelBuilder.Entity<WellnessDayTask>(builder =>
        {
            builder.ToTable("wellness_day_tasks");
            builder.HasKey(task => task.Id);
            builder.Property(task => task.Id).ValueGeneratedOnAdd();
            builder.Property(task => task.WellnessDayId).IsRequired();
            builder.Property(task => task.Name).HasMaxLength(WellnessDefaults.TaskNameMaximumLength).IsRequired();
            builder.Property(task => task.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(task => task.Completed).IsRequired();
            builder.Property(task => task.Position).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_wellness_day_tasks_category",
                    "\"Category\" IN ('HealthAndBody', 'MindAndSleep', 'PeopleAndWork')"));
            builder.HasOne<WellnessDay>().WithMany().HasForeignKey(task => task.WellnessDayId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(task => new { task.WellnessDayId, task.Position, task.Id });
        });
    }
}
