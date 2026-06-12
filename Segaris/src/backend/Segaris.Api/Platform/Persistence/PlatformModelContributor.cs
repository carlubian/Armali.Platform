using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Segaris.Api.Platform.Attachments;
using Segaris.Api.Platform.Jobs;
using Segaris.Persistence;

namespace Segaris.Api.Platform.Persistence;

internal sealed class PlatformModelContributor : ISegarisModelContributor
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureCompatibilityRecord(modelBuilder.Entity<PersistenceCompatibilityRecord>());
        ConfigureAttachment(modelBuilder.Entity<AttachmentRecord>());
        ConfigureJob(modelBuilder.Entity<JobRecord>());
    }

    private static void ConfigureCompatibilityRecord(
        EntityTypeBuilder<PersistenceCompatibilityRecord> builder)
    {
        builder.ToTable("platform_persistence_compatibility");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedOnAdd();
        builder.Property(record => record.Name).HasMaxLength(120).IsRequired();
        builder.Property(record => record.CivilDate).HasColumnType("date");
        builder.Property(record => record.Amount).HasPrecision(18, 2);
        builder.Property(record => record.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(record => record.CreatedAt).IsRequired();
        builder.HasIndex(record => record.Name).IsUnique();
    }

    private static void ConfigureAttachment(EntityTypeBuilder<AttachmentRecord> builder)
    {
        builder.ToTable("platform_attachments");
        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.Id).ValueGeneratedOnAdd();
        builder.Property(attachment => attachment.Module).HasMaxLength(80).IsRequired();
        builder.Property(attachment => attachment.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(attachment => attachment.EntityId).HasMaxLength(120).IsRequired();
        builder.Property(attachment => attachment.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(attachment => attachment.StorageFileName).HasMaxLength(80).IsRequired();
        builder.Property(attachment => attachment.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(attachment => attachment.CreatedAt).IsRequired();
        builder.HasIndex(attachment => attachment.StorageFileName).IsUnique();
        builder.HasIndex(attachment => new
        {
            attachment.Module,
            attachment.EntityType,
            attachment.EntityId,
        });
    }

    private static void ConfigureJob(EntityTypeBuilder<JobRecord> builder)
    {
        builder.ToTable("platform_background_jobs");
        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedOnAdd();
        builder.Property(job => job.JobType).HasMaxLength(80).IsRequired();
        builder.Property(job => job.State)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(job => job.ActiveExclusivityKey).HasMaxLength(80);
        builder.Property(job => job.Parameters).HasMaxLength(4000);
        builder.Property(job => job.ProgressCode).HasMaxLength(80);
        builder.Property(job => job.ResultReference).HasMaxLength(260);
        builder.Property(job => job.ResultCode).HasMaxLength(80);
        builder.Property(job => job.FailureCode).HasMaxLength(80);
        builder.Property(job => job.TraceId).HasMaxLength(120);
        builder.Property(job => job.CreatedAt).IsRequired();
        builder.Property(job => job.UpdatedAt).IsRequired();

        // One active job per exclusivity key. The column is null for terminal jobs and for
        // job types without mutual exclusion, and both providers permit multiple nulls in a
        // unique index, so this enforces single-run without a provider-specific filter.
        builder.HasIndex(job => job.ActiveExclusivityKey).IsUnique();
        builder.HasIndex(job => job.State);
    }
}
