using Blackwing.Persistence.Content;
using Blackwing.Shared.Content;
using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blackwing.Persistence.Uploads;

/// <summary>
/// One queued upload. Phase 3 only defines and migrates it; the batch endpoint, the worker and
/// the progress reporting that put it to work belong to phase 4.
/// </summary>
public sealed class UploadJob : IOwnedByUser
{
    public int Id { get; set; }

    /// <inheritdoc />
    public int OwnerUserId { get; set; }

    /// <summary>Groups the files of one batch.</summary>
    public Guid BatchId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>The size the client announced, which is not necessarily the size received.</summary>
    public long DeclaredByteSize { get; set; }

    public UploadJobState State { get; set; } = UploadJobState.Pending;

    /// <summary>Points into the staging area while the file waits to be processed.</summary>
    public string? StagedFileName { get; set; }

    public string? ContentHash { get; set; }

    /// <summary>The resulting image, null until the job completes.</summary>
    public int? ImageId { get; set; }

    /// <summary>A machine-readable failure code, null unless the job failed.</summary>
    public string? FailureCode { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}

internal sealed class UploadJobConfiguration : IEntityTypeConfiguration<UploadJob>
{
    public void Configure(EntityTypeBuilder<UploadJob> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("upload_jobs");
        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedOnAdd();
        builder.Property(job => job.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(job => job.State)
            .HasConversion<int>()
            .HasDefaultValue(UploadJobState.Pending);
        builder.Property(job => job.StagedFileName).HasMaxLength(128);
        builder.Property(job => job.ContentHash).HasMaxLength(64);
        builder.Property(job => job.FailureCode).HasMaxLength(64);
        builder.Property(job => job.AttemptCount).HasDefaultValue(0);
        builder.Property(job => job.CreatedAt).IsRequired();

        // Deleting an image must not erase the history of how it was uploaded.
        builder.HasOne<Image>()
            .WithMany()
            .HasForeignKey(job => job.ImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(job => new { job.OwnerUserId, job.BatchId, job.Id })
            .HasDatabaseName("IX_upload_jobs_owner_batch_id");
        builder.HasIndex(job => new { job.State, job.Id })
            .HasDatabaseName("IX_upload_jobs_state_id");
    }
}
