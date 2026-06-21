using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Processes.Persistence;

internal sealed class ProcessesModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessCategory>(builder =>
        {
            builder.ToTable("processes_categories");
            builder.HasKey(category => category.Id);
            builder.Property(category => category.Id).ValueGeneratedOnAdd();
            builder.Property(category => category.Name).HasMaxLength(ProcessesValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.NormalizedName).HasMaxLength(ProcessesValidation.CategoryNameMaximumLength).IsRequired();
            builder.Property(category => category.SortOrder).IsRequired();
            builder.Property(category => category.CreatedAt).IsRequired();
            builder.Property(category => category.CreatedBy);
            builder.Property(category => category.UpdatedAt).IsRequired();
            builder.Property(category => category.UpdatedBy);
            builder.HasIndex(category => category.NormalizedName).IsUnique();
            builder.HasIndex(category => category.SortOrder);
        });

        modelBuilder.Entity<Process>(builder =>
        {
            builder.ToTable("processes_processes");
            builder.HasKey(process => process.Id);
            builder.Property(process => process.Id).ValueGeneratedOnAdd();
            builder.Property(process => process.Name).HasMaxLength(ProcessesValidation.NameMaximumLength).IsRequired();
            builder.Property(process => process.CategoryId).IsRequired();
            builder.Property(process => process.DueDate);
            builder.Property(process => process.Notes).HasMaxLength(ProcessesValidation.NotesMaximumLength);
            builder.Property(process => process.IsCancelled).IsRequired();
            builder.Property(process => process.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(process => process.CreatedAt).IsRequired();
            builder.Property(process => process.UpdatedAt).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint("CK_processes_processes_visibility", "\"Visibility\" IN ('Public', 'Private')"));
            builder.HasOne<ProcessCategory>().WithMany().HasForeignKey(process => process.CategoryId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(process => process.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(process => process.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default ordering (global due date with nulls last, then identifier) and access.
            builder.HasIndex(process => new { process.DueDate, process.Id });
            builder.HasIndex(process => new { process.CreatedBy, process.Visibility, process.Id });
            // Launcher attention scans accessible open processes by due date.
            builder.HasIndex(process => new { process.IsCancelled, process.DueDate });
            // Exact filters and category reference migration.
            builder.HasIndex(process => process.CategoryId);
            builder.HasIndex(process => process.Visibility);
            builder.HasIndex(process => process.UpdatedBy);
        });

        modelBuilder.Entity<Step>(builder =>
        {
            builder.ToTable("processes_steps");
            builder.HasKey(step => step.Id);
            builder.Property(step => step.Id).ValueGeneratedOnAdd();
            builder.Property(step => step.ProcessId).IsRequired();
            builder.Property(step => step.Description).HasMaxLength(ProcessesValidation.StepDescriptionMaximumLength).IsRequired();
            builder.Property(step => step.DueDate);
            builder.Property(step => step.Notes).HasMaxLength(ProcessesValidation.StepNotesMaximumLength);
            builder.Property(step => step.IsOptional).IsRequired();
            builder.Property(step => step.State).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(step => step.SortOrder).IsRequired();
            builder.Property(step => step.CreatedAt).IsRequired();
            builder.Property(step => step.UpdatedAt).IsRequired();
            builder.ToTable(table =>
                table.HasCheckConstraint("CK_processes_steps_state", "\"State\" IN ('Pending', 'Completed', 'Skipped')"));
            // Owned steps are removed when their process is deleted.
            builder.HasOne<Process>().WithMany().HasForeignKey(step => step.ProcessId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(step => step.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(step => step.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Step reads are ordered by SortOrder within a process; the attention query
            // reads the frontier step's due date.
            builder.HasIndex(step => new { step.ProcessId, step.SortOrder, step.Id });
            builder.HasIndex(step => step.DueDate);
            builder.HasIndex(step => step.UpdatedBy);
        });
    }
}
