using System.Buffers.Text;
using System.Text;
using Blackwing.Persistence;
using Blackwing.Persistence.Gallery;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Gallery;

/// <summary>
/// Owner-scoped, keyset-paginated reads for the gallery. Never loads the whole
/// collection: each page is one indexed range scan ordered by effective capture
/// date (newest first), and multi-tag filters apply closed AND semantics — an
/// image must carry every selected tag.
/// </summary>
public sealed class GalleryReadService(BlackwingDbContext database)
{
    public const int DefaultPageSize = 60;
    public const int MaxPageSize = 120;

    public async Task<GalleryPage> BrowseAsync(
        Guid ownerUserId,
        IReadOnlyCollection<Guid> tagIds,
        ReviewFilter review,
        string? cursor,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize);

        var query = database.Images.Where(image => image.OwnerUserId == ownerUserId);
        query = review switch
        {
            ReviewFilter.Pending => query.Where(image => image.ReviewedAt == null),
            ReviewFilter.Reviewed => query.Where(image => image.ReviewedAt != null),
            _ => query,
        };

        // AND semantics: one EXISTS per selected tag, each backed by the
        // (TagId, ImageId) index. Distinct guards against a repeated id.
        foreach (var tagId in tagIds.Distinct())
            query = query.Where(image => database.ImageTags.Any(link => link.ImageId == image.Id && link.TagId == tagId));

        if (GalleryCursor.TryDecode(cursor, out var keyDate, out var keyId))
        {
            // Newest first: continue strictly past the last item seen.
            query = query.Where(image =>
                image.EffectiveCapturedAt < keyDate
                || (image.EffectiveCapturedAt == keyDate && image.Id.CompareTo(keyId) < 0));
        }

        var rows = await query
            .OrderByDescending(image => image.EffectiveCapturedAt).ThenByDescending(image => image.Id)
            .Take(pageSize + 1) // One extra row tells us whether another page exists.
            .Select(image => new GalleryItem(
                image.Id, image.Width, image.Height, image.CapturedAt, image.UploadedAt,
                image.EffectiveCapturedAt, image.ReviewedAt != null))
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (rows.Count > pageSize)
        {
            var last = rows[pageSize - 1];
            nextCursor = GalleryCursor.Encode(last.EffectiveCapturedAt, last.Id);
            rows.RemoveAt(pageSize);
        }

        return new GalleryPage(rows, nextCursor);
    }

    /// <summary>
    /// The owner's tags grouped for the filter sidebar, each with how many of the
    /// owner's images carry it. Ordered by value within a type.
    /// </summary>
    public async Task<IReadOnlyList<TagFacet>> TagFacetsAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        await database.Tags
            .Where(tag => tag.OwnerUserId == ownerUserId)
            .OrderBy(tag => tag.Value)
            .Select(tag => new TagFacet(
                tag.Id,
                tag.Type.ToString(),
                tag.Value,
                database.ImageTags.Count(link => link.TagId == tag.Id)))
            .ToListAsync(cancellationToken);
}

/// <summary>Which review states a gallery request includes.</summary>
public enum ReviewFilter
{
    All,
    Pending,
    Reviewed,
}

/// <summary>Opaque, stable keyset cursor over (effective capture date, id).</summary>
internal static class GalleryCursor
{
    public static string Encode(DateTimeOffset effectiveCapturedAt, Guid id) =>
        Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{effectiveCapturedAt.UtcDateTime:O}|{id:N}"));

    public static bool TryDecode(string? cursor, out DateTimeOffset effectiveCapturedAt, out Guid id)
    {
        effectiveCapturedAt = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor)) return false;
        try
        {
            var text = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
            var separator = text.IndexOf('|');
            if (separator < 0) return false;
            if (!DateTimeOffset.TryParse(text[..separator], null, System.Globalization.DateTimeStyles.RoundtripKind, out effectiveCapturedAt)) return false;
            return Guid.TryParseExact(text[(separator + 1)..], "N", out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record GalleryItem(
    Guid Id, int Width, int Height, DateTimeOffset? CapturedAt, DateTimeOffset UploadedAt,
    DateTimeOffset EffectiveCapturedAt, bool Reviewed);

public sealed record GalleryPage(IReadOnlyList<GalleryItem> Items, string? NextCursor);

public sealed record TagFacet(Guid Id, string Type, string Value, int Count);
