using Blackwing.Persistence;
using Blackwing.Persistence.Gallery;
using Blackwing.Shared.Storage;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Gallery;

/// <summary>
/// The transactional building blocks for managing a user's own gallery. Every
/// operation is scoped to the acting owner and never touches another user's rows;
/// each multi-step change runs inside a database transaction so a failure leaves
/// no partial state. Endpoints on top of these arrive in later phases.
/// </summary>
public sealed class GalleryMutationService(BlackwingDbContext database, IImageStore imageStore)
{
    /// <summary>
    /// Replaces an image's tags with owner-scoped values, creating missing tags,
    /// and optionally completes its review in the same transaction.
    /// </summary>
    public async Task<bool> SetImageTagValuesAsync(
        Guid imageId,
        Guid ownerUserId,
        IReadOnlyCollection<TagValue> tags,
        bool markReviewed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);
        var image = await database.Images.FirstOrDefaultAsync(value => value.Id == imageId && value.OwnerUserId == ownerUserId, cancellationToken);
        if (image is null) return false;

        var requested = tags
            .Select(value => new { value.Type, Value = value.Value?.Trim() ?? string.Empty })
            .Where(value => value.Value.Length > 0)
            .DistinctBy(value => new { value.Type, Normalized = TagNormalization.Normalize(value.Value) })
            .ToList();
        if (requested.Count != tags.Count) throw new ArgumentException("Tags must be non-empty and unique within their type.", nameof(tags));

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var tagIds = new List<Guid>(requested.Count);
        foreach (var requestedTag in requested)
        {
            var normalized = TagNormalization.Normalize(requestedTag.Value);
            var tag = await FindTagAsync(ownerUserId, requestedTag.Type, normalized, cancellationToken);
            if (tag is null)
            {
                tag = Tag.Create(ownerUserId, requestedTag.Type, requestedTag.Value);
                database.Tags.Add(tag);
                await database.SaveChangesAsync(cancellationToken);
            }
            tagIds.Add(tag.Id);
        }

        var existing = await database.ImageTags.Where(link => link.ImageId == imageId).ToListAsync(cancellationToken);
        database.ImageTags.RemoveRange(existing);
        foreach (var tagId in tagIds) database.ImageTags.Add(ImageTag.Link(imageId, tagId));
        if (markReviewed) image.MarkReviewed(DateTimeOffset.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resolves the owner's tag with the given type and (normalized) value,
    /// creating it when it does not yet exist. Tolerates a concurrent create by
    /// re-reading after a unique-constraint violation.
    /// </summary>
    public async Task<Tag> GetOrCreateTagAsync(Guid ownerUserId, TagType type, string value, CancellationToken cancellationToken = default)
    {
        var normalized = TagNormalization.Normalize(value);
        var existing = await FindTagAsync(ownerUserId, type, normalized, cancellationToken);
        if (existing is not null) return existing;

        var tag = Tag.Create(ownerUserId, type, value);
        database.Tags.Add(tag);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return tag;
        }
        catch (DbUpdateException)
        {
            database.Entry(tag).State = EntityState.Detached;
            var raced = await FindTagAsync(ownerUserId, type, normalized, cancellationToken);
            if (raced is null) throw;
            return raced;
        }
    }

    /// <summary>
    /// Replaces the set of tags associated with one of the owner's images. All
    /// tags must belong to the same owner. Returns <c>false</c> when the image is
    /// not the owner's.
    /// </summary>
    public async Task<bool> SetImageTagsAsync(Guid imageId, Guid ownerUserId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagIds);
        var owns = await database.Images.AnyAsync(image => image.Id == imageId && image.OwnerUserId == ownerUserId, cancellationToken);
        if (!owns) return false;

        var distinct = tagIds.Distinct().ToList();
        if (distinct.Count > 0)
        {
            var ownedCount = await database.Tags.CountAsync(tag => distinct.Contains(tag.Id) && tag.OwnerUserId == ownerUserId, cancellationToken);
            if (ownedCount != distinct.Count) throw new InvalidOperationException("Every tag must belong to the image owner.");
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var existing = await database.ImageTags.Where(link => link.ImageId == imageId).ToListAsync(cancellationToken);
        database.ImageTags.RemoveRange(existing);
        foreach (var tagId in distinct) database.ImageTags.Add(ImageTag.Link(imageId, tagId));
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Deletes one of the owner's images: its tag associations, its stored
    /// derivatives, and any of the owner's tags left orphaned by the deletion.
    /// Returns <c>false</c> when the image is not the owner's.
    /// </summary>
    public async Task<bool> DeleteImageAsync(Guid imageId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var image = await database.Images.FirstOrDefaultAsync(value => value.Id == imageId && value.OwnerUserId == ownerUserId, cancellationToken);
        if (image is null) return false;

        var linkedTagIds = await database.ImageTags.Where(link => link.ImageId == imageId).Select(link => link.TagId).ToListAsync(cancellationToken);

        await using (var transaction = await database.Database.BeginTransactionAsync(cancellationToken))
        {
            database.Images.Remove(image); // Cascades the image's ImageTag rows.
            await database.SaveChangesAsync(cancellationToken);

            if (linkedTagIds.Count > 0)
            {
                var orphaned = await database.Tags
                    .Where(tag => linkedTagIds.Contains(tag.Id)
                        && tag.OwnerUserId == ownerUserId
                        && !database.ImageTags.Any(link => link.TagId == tag.Id))
                    .ToListAsync(cancellationToken);
                database.Tags.RemoveRange(orphaned);
                await database.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        // Files are removed only after the row is durably gone, so a failure here
        // leaves harmless orphan bytes rather than a record pointing at nothing.
        await imageStore.DeleteAllAsync(ownerUserId, image.Sha256, cancellationToken);
        return true;
    }

    /// <summary>
    /// Merges tag <paramref name="sourceTagId"/> into <paramref name="targetTagId"/>
    /// across all of the owner's images, then deletes the source. Both tags must
    /// belong to the owner and share the same type. Returns <c>false</c> when
    /// either tag is missing for the owner.
    /// </summary>
    public async Task<bool> MergeTagsAsync(Guid sourceTagId, Guid targetTagId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        if (sourceTagId == targetTagId) throw new InvalidOperationException("A tag cannot be merged into itself.");
        var source = await database.Tags.FirstOrDefaultAsync(tag => tag.Id == sourceTagId && tag.OwnerUserId == ownerUserId, cancellationToken);
        var target = await database.Tags.FirstOrDefaultAsync(tag => tag.Id == targetTagId && tag.OwnerUserId == ownerUserId, cancellationToken);
        if (source is null || target is null) return false;
        if (source.Type != target.Type) throw new InvalidOperationException("Tags of different types cannot be merged.");

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var targetImageIds = await database.ImageTags.Where(link => link.TagId == targetTagId).Select(link => link.ImageId).ToListAsync(cancellationToken);
        var targetImages = targetImageIds.ToHashSet();
        var sourceLinks = await database.ImageTags.Where(link => link.TagId == sourceTagId).ToListAsync(cancellationToken);

        // Repoint by delete-then-insert (ImageId/TagId form the composite key),
        // dropping any link that would duplicate one the target already holds.
        database.ImageTags.RemoveRange(sourceLinks);
        await database.SaveChangesAsync(cancellationToken);
        foreach (var link in sourceLinks.Where(link => !targetImages.Contains(link.ImageId)))
            database.ImageTags.Add(ImageTag.Link(link.ImageId, targetTagId));
        database.Tags.Remove(source);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private Task<Tag?> FindTagAsync(Guid ownerUserId, TagType type, string normalizedValue, CancellationToken cancellationToken) =>
        database.Tags.FirstOrDefaultAsync(
            tag => tag.OwnerUserId == ownerUserId && tag.Type == type && tag.NormalizedValue == normalizedValue,
            cancellationToken);
}

public sealed record TagValue(TagType Type, string Value);
