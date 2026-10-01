using Blackwing.Shared.Content;
using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blackwing.Persistence.Content;

/// <summary>
/// One photo in one account's library. The database holds the content hash and the metadata,
/// never a file path: where the bytes live is the business of the image blob store.
/// </summary>
public sealed class Image : IOwnedByUser
{
    public int Id { get; set; }

    /// <inheritdoc />
    public int OwnerUserId { get; set; }

    /// <summary>SHA-256 of the original content, 64 lowercase hexadecimal characters.</summary>
    public string ContentHash { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long ByteSize { get; set; }

    /// <summary>Width after the EXIF orientation has been applied.</summary>
    public int Width { get; set; }

    /// <summary>Height after the EXIF orientation has been applied.</summary>
    public int Height { get; set; }

    /// <summary>Capture date from EXIF, read as UTC because EXIF carries no zone. Null when absent.</summary>
    public DateTimeOffset? CapturedAt { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// <c>COALESCE(CapturedAt, UploadedAt)</c>, computed and stored by PostgreSQL. Nothing writes
    /// it, so it cannot drift out of step with the two columns it derives from.
    /// </summary>
    public DateTimeOffset SortedAt { get; set; }

    public ImageReviewState ReviewState { get; set; } = ImageReviewState.Pending;

    public DateTimeOffset? ReviewedAt { get; set; }

    public ICollection<ImageTag> ImageTags { get; set; } = [];
}

internal sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("images");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id).ValueGeneratedOnAdd();
        builder.Property(image => image.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(image => image.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(image => image.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(image => image.ByteSize).IsRequired();
        builder.Property(image => image.UploadedAt).IsRequired();

        builder.Property(image => image.SortedAt)
            .HasComputedColumnSql("COALESCE(\"CapturedAt\", \"UploadedAt\")", stored: true)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(image => image.ReviewState)
            .HasConversion<int>()
            .HasDefaultValue(ImageReviewState.Pending);

        // What rejects an exact duplicate per account. Different accounts may of course hold the
        // same bytes: the privacy model prefers duplicating them to sharing them.
        builder.HasIndex(image => new { image.OwnerUserId, image.ContentHash })
            .IsUnique()
            .HasDatabaseName("IX_images_owner_content_hash");

        // The gallery's keyset cursor (phase 6): newest first, with the identifier breaking ties.
        builder.HasIndex(image => new { image.OwnerUserId, image.SortedAt, image.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_images_owner_sorted_at_id");

        // The review queue (phase 5).
        builder.HasIndex(image => new { image.OwnerUserId, image.ReviewState, image.SortedAt, image.Id })
            .HasDatabaseName("IX_images_owner_review_state_sorted_at_id");
    }
}
