using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Projects.Persistence;

internal sealed class ProjectsModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectProgram>(builder =>
        {
            builder.ToTable("projects_programs");
            builder.HasKey(program => program.Id);
            builder.Property(program => program.Id).ValueGeneratedOnAdd();
            builder.Property(program => program.Name).HasMaxLength(ProjectsValidation.NameMaximumLength).IsRequired();
            builder.Property(program => program.Code).HasMaxLength(ProjectsValidation.CodeLength).IsRequired();
            builder.Property(program => program.CreatedAt).IsRequired();
            builder.Property(program => program.UpdatedAt).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_projects_programs_code",
                    "length(\"Code\") = 4 AND \"Code\" = upper(\"Code\")"));
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(program => program.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(program => program.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(program => program.Code).IsUnique();
            builder.HasIndex(program => new { program.Code, program.Id });
            builder.HasIndex(program => program.UpdatedBy);
        });

        modelBuilder.Entity<ProjectAxis>(builder =>
        {
            builder.ToTable("projects_axes");
            builder.HasKey(axis => axis.Id);
            builder.Property(axis => axis.Id).ValueGeneratedOnAdd();
            builder.Property(axis => axis.ProgramId).IsRequired();
            builder.Property(axis => axis.Name).HasMaxLength(ProjectsValidation.NameMaximumLength).IsRequired();
            builder.Property(axis => axis.Code).HasMaxLength(ProjectsValidation.CodeLength).IsRequired();
            builder.Property(axis => axis.CreatedAt).IsRequired();
            builder.Property(axis => axis.UpdatedAt).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_projects_axes_code",
                    "length(\"Code\") = 4 AND \"Code\" = upper(\"Code\")"));
            builder.HasOne<ProjectProgram>().WithMany().HasForeignKey(axis => axis.ProgramId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(axis => axis.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(axis => axis.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(axis => axis.Code).IsUnique();
            builder.HasIndex(axis => new { axis.ProgramId, axis.Code, axis.Id });
            builder.HasIndex(axis => axis.UpdatedBy);
        });

        ConfigureProjectItem<Project>(modelBuilder, "projects_projects");
        ConfigureProjectItem<Activity>(modelBuilder, "projects_activities");
        ConfigureProjectRisk(modelBuilder);

        modelBuilder.Entity<ProjectNumberAllocation>(builder =>
        {
            builder.ToTable("projects_number_allocations");
            builder.HasKey(allocation => allocation.Id);
            builder.Property(allocation => allocation.Id).ValueGeneratedOnAdd();
            builder.Property(allocation => allocation.AllocatedAt).IsRequired();
        });
    }

    private static void ConfigureProjectItem<TItem>(ModelBuilder modelBuilder, string tableName)
        where TItem : ProjectItem
    {
        modelBuilder.Entity<TItem>(builder =>
        {
            builder.ToTable(tableName);
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Id).ValueGeneratedOnAdd();
            builder.Property(item => item.AxisId).IsRequired();
            builder.Property(item => item.Number).IsRequired();
            builder.Property(item => item.Name).HasMaxLength(ProjectsValidation.NameMaximumLength).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(item => item.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(item => item.CreatedAt).IsRequired();
            builder.Property(item => item.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    $"CK_{tableName}_status",
                    "\"Status\" IN ('Planning', 'Active', 'Completed', 'OnHold', 'Cancelled')");
                table.HasCheckConstraint(
                    $"CK_{tableName}_visibility",
                    "\"Visibility\" IN ('Public', 'Private')");
                table.HasCheckConstraint($"CK_{tableName}_number", "\"Number\" > 0");
            });
            builder.HasOne<ProjectAxis>().WithMany().HasForeignKey(item => item.AxisId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(item => item.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(item => item.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(item => item.Number).IsUnique();
            builder.HasIndex(item => new { item.AxisId, item.Number, item.Id });
            builder.HasIndex(item => new { item.CreatedBy, item.Visibility, item.Id });
            builder.HasIndex(item => item.Visibility);
            builder.HasIndex(item => item.UpdatedBy);
        });
    }

    private static void ConfigureProjectRisk(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectRisk>(builder =>
        {
            builder.ToTable("projects_risks");
            builder.HasKey(risk => risk.Id);
            builder.Property(risk => risk.Id).ValueGeneratedOnAdd();
            builder.Property(risk => risk.ProjectId).IsRequired();
            builder.Property(risk => risk.Description).HasMaxLength(ProjectsValidation.RiskDescriptionMaximumLength).IsRequired();
            builder.Property(risk => risk.Probability).IsRequired();
            builder.Property(risk => risk.Impact).IsRequired();
            builder.Property(risk => risk.Mitigation).IsRequired();
            builder.Property(risk => risk.Score).IsRequired();
            builder.Property(risk => risk.CreatedAt).IsRequired();
            builder.Property(risk => risk.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_projects_risks_probability",
                    "\"Probability\" BETWEEN 1 AND 5");
                table.HasCheckConstraint(
                    "CK_projects_risks_impact",
                    "\"Impact\" BETWEEN 1 AND 5");
                table.HasCheckConstraint(
                    "CK_projects_risks_mitigation",
                    "\"Mitigation\" BETWEEN 1 AND 5");
                table.HasCheckConstraint(
                    "CK_projects_risks_score",
                    "\"Score\" BETWEEN 1 AND 125");
            });
            builder.HasOne<Project>().WithMany().HasForeignKey(risk => risk.ProjectId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(risk => risk.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(risk => risk.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(risk => new { risk.ProjectId, risk.Id });
            builder.HasIndex(risk => risk.Score);
            builder.HasIndex(risk => risk.UpdatedBy);
        });
    }
}
