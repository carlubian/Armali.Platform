namespace Blackwing.Persistence.Gallery;

/// <summary>
/// The many-to-many join between an image and a tag. Both sides share the same
/// owner (enforced by the write services, never crossing users), so the join
/// carries no owner column of its own.
/// </summary>
public sealed class ImageTag
{
    private ImageTag()
    {
    }

    public Guid ImageId { get; private set; }
    public Guid TagId { get; private set; }

    public static ImageTag Link(Guid imageId, Guid tagId)
    {
        if (imageId == Guid.Empty) throw new ArgumentException("An image is required.", nameof(imageId));
        if (tagId == Guid.Empty) throw new ArgumentException("A tag is required.", nameof(tagId));
        return new ImageTag { ImageId = imageId, TagId = tagId };
    }
}
