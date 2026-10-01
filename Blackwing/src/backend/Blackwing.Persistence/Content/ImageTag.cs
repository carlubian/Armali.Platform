using Blackwing.Shared.Ownership;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blackwing.Persistence.Content;

/// <summary>
/// The many-to-many link between an image and a tag, explicit so it can be owned too. A link
/// table that was not <see cref="IOwnedByUser"/> would let a query that starts from it step
/// outside the privacy perimeter.
/// </summary>
public sealed class ImageTag : IOwnedByUser
{
    public int ImageId { get; set; }

    public int TagId { get; set; }

    /// <inheritdoc />
    public int OwnerUserId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public Image Image { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}

internal sealed class ImageTagConfiguration : IEntityTypeConfiguration<ImageTag>
{
    public void Configure(EntityTypeBuilder<ImageTag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("image_tags");
        builder.HasKey(link => new { link.ImageId, link.TagId });
        builder.Property(link => link.AssignedAt).IsRequired();

        // Deleting an image takes its associations with it, and so does deleting a tag. Neither
        // deletes the other side.
        builder.HasOne(link => link.Image)
            .WithMany(image => image.ImageTags)
            .HasForeignKey(link => link.ImageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Tag)
            .WithMany(tag => tag.ImageTags)
            .HasForeignKey(link => link.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // The entry point of the AND filter of phase 7: images carrying a given tag, per owner.
        builder.HasIndex(link => new { link.OwnerUserId, link.TagId, link.ImageId })
            .HasDatabaseName("IX_image_tags_owner_tag_image");
    }
}
