using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Maintenance.Persistence;

internal sealed class MaintenanceModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceType>(builder =>
        {
            builder.ToTable("maintenance_types");
            builder.HasKey(type => type.Id);
            builder.Property(type => type.Id).ValueGeneratedOnAdd();
            builder.Property(type => type.Name).HasMaxLength(MaintenanceValidation.TypeNameMaximumLength).IsRequired();
            builder.Property(type => type.NormalizedName).HasMaxLength(MaintenanceValidation.TypeNameMaximumLength).IsRequired();
            builder.Property(type => type.SortOrder).IsRequired();
            builder.Property(type => type.CreatedAt).IsRequired();
            builder.Property(type => type.CreatedBy);
            builder.Property(type => type.UpdatedAt).IsRequired();
            builder.Property(type => type.UpdatedBy);
            builder.HasIndex(type => type.NormalizedName).IsUnique();
            builder.HasIndex(type => type.SortOrder);
        });

        modelBuilder.Entity<MaintenanceTask>(builder =>
        {
            builder.ToTable("maintenance_tasks");
            builder.HasKey(task => task.Id);
            builder.Property(task => task.Id).ValueGeneratedOnAdd();
            builder.Property(task => task.Title)
                .HasMaxLength(MaintenanceValidation.TitleMaximumLength).IsRequired();
            builder.Property(task => task.MaintenanceTypeId).IsRequired();
            builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(task => task.Priority).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(task => task.DueDate);
            builder.Property(task => task.CompletedDate);
            builder.Property(task => task.Notes).HasMaxLength(MaintenanceValidation.NotesMaximumLength);
            // The asset reference is a stable opaque identifier resolved live through the
            // Assets read contract. It is deliberately not modelled as a foreign key so
            // the dependency direction stays Maintenance to Assets; its integrity is
            // preserved by the Assets deletion guard rather than a database constraint.
            builder.Property(task => task.AssetId);
            builder.Property(task => task.Visibility).HasConversion<string>().HasMaxLength(10).IsRequired();
            builder.Property(task => task.CreatedAt).IsRequired();
            builder.Property(task => task.UpdatedAt).IsRequired();
            builder.ToTable(table =>
            {
                table.HasCheckConstraint("CK_maintenance_tasks_status", "\"Status\" IN ('Pending', 'InProgress', 'Completed', 'Cancelled')");
                table.HasCheckConstraint("CK_maintenance_tasks_priority", "\"Priority\" IN ('Low', 'Medium', 'High')");
                table.HasCheckConstraint("CK_maintenance_tasks_visibility", "\"Visibility\" IN ('Public', 'Private')");
            });
            builder.HasOne<MaintenanceType>().WithMany().HasForeignKey(task => task.MaintenanceTypeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(task => task.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<SegarisUser>().WithMany().HasForeignKey(task => task.UpdatedBy).OnDelete(DeleteBehavior.Restrict);
            // Default ordering (due date with nulls last, then identifier) and access.
            builder.HasIndex(task => new { task.DueDate, task.Id });
            builder.HasIndex(task => new { task.CreatedBy, task.Visibility, task.Id });
            // Launcher attention scans accessible open tasks by due date.
            builder.HasIndex(task => new { task.Status, task.DueDate });
            // Exact filters, type reference migration, and the asset-reference lookup
            // used by the Assets deletion guard.
            builder.HasIndex(task => task.MaintenanceTypeId);
            builder.HasIndex(task => task.AssetId);
            builder.HasIndex(task => task.Priority);
            builder.HasIndex(task => task.Visibility);
        });
    }
}
