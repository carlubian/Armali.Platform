using Blackwing.Persistence;
using Blackwing.Persistence.Gallery;
using Blackwing.Shared.Ownership;
using Blackwing.Shared.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace Blackwing.Api.Gallery;

/// <summary>Private, owner-scoped endpoints for browsing, review, tag maintenance and image delivery.</summary>
public static class GalleryEndpoints
{
    public static IEndpointRouteBuilder MapGalleryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/images").RequireAuthorization();
        group.MapGet("/", Browse);
        group.MapGet("/review", GetNextForReview);
        group.MapGet("/{id:guid}", GetImage);
        group.MapPut("/{id:guid}/tags", SetTags);
        group.MapPost("/{id:guid}/review", MarkReviewed);
        group.MapDelete("/{id:guid}", DeleteImage);
        group.MapGet("/{id:guid}/thumb", (Guid id, HttpContext http, IUserScope userScope, BlackwingDbContext database, IImageStore store, CancellationToken cancellationToken) =>
            StreamDerivative(id, ImageDerivative.Thumbnail, asDownload: false, http, userScope, database, store, cancellationToken));
        group.MapGet("/{id:guid}/preview", (Guid id, HttpContext http, IUserScope userScope, BlackwingDbContext database, IImageStore store, CancellationToken cancellationToken) =>
            StreamDerivative(id, ImageDerivative.Preview, asDownload: false, http, userScope, database, store, cancellationToken));
        group.MapGet("/{id:guid}/original", (Guid id, HttpContext http, IUserScope userScope, BlackwingDbContext database, IImageStore store, CancellationToken cancellationToken) =>
            StreamDerivative(id, ImageDerivative.Original, asDownload: true, http, userScope, database, store, cancellationToken));

        var tags = endpoints.MapGroup("/api/tags").RequireAuthorization();
        tags.MapGet("/", Autocomplete);
        tags.MapGet("/facets", Facets);
        tags.MapPost("/{sourceId:guid}/merge", Merge);
        return endpoints;
    }

    private static async Task<IResult> Browse(
        string? status, string? cursor, int? limit,
        IUserScope userScope, GalleryReadService gallery, HttpContext http, CancellationToken cancellationToken)
    {
        // Read tag ids straight from the query so a repeated ?tag= binds every value.
        var tagIds = ParseTagIds(http.Request.Query["tag"]);
        if (tagIds is null) return Results.BadRequest(new { error = "Every tag filter must be a valid id." });
        if (!TryParseReviewFilter(status, out var review)) return Results.BadRequest(new { error = "Unknown review status filter." });
        var page = await gallery.BrowseAsync(userScope.UserId, tagIds, review, cursor, limit, cancellationToken);
        return Results.Ok(page);
    }

    private static async Task<IResult> Facets(IUserScope userScope, GalleryReadService gallery, CancellationToken cancellationToken) =>
        Results.Ok(new { tags = await gallery.TagFacetsAsync(userScope.UserId, cancellationToken) });

    private static async Task<IResult> GetNextForReview(IUserScope userScope, BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var owner = userScope.UserId;
        var pendingCount = await database.Images.CountAsync(image => image.OwnerUserId == owner && image.ReviewedAt == null, cancellationToken);
        var image = await database.Images
            .Where(value => value.OwnerUserId == owner && value.ReviewedAt == null)
            .OrderBy(value => value.UploadedAt).ThenBy(value => value.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return Results.Ok(new ReviewResponse(pendingCount, image is null ? null : await ToViewAsync(image, database, cancellationToken)));
    }

    private static async Task<IResult> GetImage(Guid id, IUserScope userScope, BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var image = await database.Images.FirstOrDefaultAsync(value => value.Id == id && value.OwnerUserId == userScope.UserId, cancellationToken);
        return image is null ? Results.NotFound() : Results.Ok(await ToViewAsync(image, database, cancellationToken));
    }

    private static async Task<IResult> SetTags(Guid id, UpdateImageTagsRequest request, IUserScope userScope, GalleryMutationService service, CancellationToken cancellationToken)
    {
        if (request.Tags is null) return Results.BadRequest(new { error = "Tags are required." });
        try
        {
            var tagValues = request.Tags.Select(tag => new TagValue(ParseTagType(tag.Type), tag.Value)).ToList();
            var updated = await service.SetImageTagValuesAsync(id, userScope.UserId, tagValues, request.MarkReviewed, cancellationToken);
            return updated ? Results.NoContent() : Results.NotFound();
        }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
    }

    private static async Task<IResult> MarkReviewed(Guid id, IUserScope userScope, BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var image = await database.Images.FirstOrDefaultAsync(value => value.Id == id && value.OwnerUserId == userScope.UserId, cancellationToken);
        if (image is null) return Results.NotFound();
        image.MarkReviewed(DateTimeOffset.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteImage(Guid id, IUserScope userScope, GalleryMutationService service, CancellationToken cancellationToken) =>
        await service.DeleteImageAsync(id, userScope.UserId, cancellationToken) ? Results.NoContent() : Results.NotFound();

    /// <summary>
    /// Streams one derivative after verifying ownership. Never exposes files as
    /// static content: the id is resolved to the owner's row first. Derivatives are
    /// immutable (addressed by content hash), so responses carry a strong ETag and
    /// long-lived private caching, and support HTTP range requests for the original.
    /// </summary>
    private static async Task<IResult> StreamDerivative(
        Guid id, ImageDerivative derivative, bool asDownload,
        HttpContext http, IUserScope userScope, BlackwingDbContext database, IImageStore store, CancellationToken cancellationToken)
    {
        var image = await database.Images.FirstOrDefaultAsync(value => value.Id == id && value.OwnerUserId == userScope.UserId, cancellationToken);
        if (image is null) return Results.NotFound();

        var stream = await store.OpenReadAsync(userScope.UserId, image.Sha256, derivative, cancellationToken);
        if (stream is null) return Results.NotFound();

        http.Response.Headers.CacheControl = "private, immutable, max-age=31536000";
        var entityTag = new EntityTagHeaderValue($"\"{image.Sha256}-{(int)derivative}\"");
        var contentType = derivative == ImageDerivative.Original ? image.ContentType : "image/webp";
        var downloadName = asDownload ? DownloadName(image, derivative) : null;
        return Results.File(stream, contentType, downloadName, lastModified: image.UploadedAt, entityTag: entityTag, enableRangeProcessing: true);
    }

    private static string DownloadName(Image image, ImageDerivative derivative)
    {
        var extension = derivative == ImageDerivative.Original
            ? image.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => string.Empty,
            }
            : ".webp";
        return $"blackwing-{image.Id:N}{extension}";
    }

    private static async Task<IResult> Autocomplete(string? type, string? query, IUserScope userScope, BlackwingDbContext database, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TagType>(type, true, out var tagType)) return Results.BadRequest(new { error = "A valid tag type is required." });
        var normalized = TagNormalization.Normalize(query ?? string.Empty);
        var values = await database.Tags.Where(tag => tag.OwnerUserId == userScope.UserId && tag.Type == tagType && tag.NormalizedValue.StartsWith(normalized))
            .OrderBy(tag => tag.Value).Take(10).Select(tag => new TagView(tag.Id, tag.Type.ToString(), tag.Value)).ToListAsync(cancellationToken);
        return Results.Ok(new { tags = values });
    }

    private static async Task<IResult> Merge(Guid sourceId, MergeTagsRequest request, IUserScope userScope, GalleryMutationService service, CancellationToken cancellationToken)
    {
        try { return await service.MergeTagsAsync(sourceId, request.TargetTagId, userScope.UserId, cancellationToken) ? Results.NoContent() : Results.NotFound(); }
        catch (InvalidOperationException exception) { return Results.BadRequest(new { error = exception.Message }); }
    }

    private static async Task<ImageView> ToViewAsync(Image image, BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var tags = await (from link in database.ImageTags join tag in database.Tags on link.TagId equals tag.Id
                          where link.ImageId == image.Id select new TagView(tag.Id, tag.Type.ToString(), tag.Value)).OrderBy(tag => tag.Type).ThenBy(tag => tag.Value).ToListAsync(cancellationToken);
        return new ImageView(image.Id, image.Width, image.Height, image.Bytes, image.CapturedAt, image.UploadedAt, image.ReviewedAt, tags);
    }

    private static TagType ParseTagType(string value) =>
        Enum.TryParse<TagType>(value, true, out var type) ? type : throw new ArgumentException("A valid tag type is required.", nameof(value));

    private static List<Guid>? ParseTagIds(IEnumerable<string?> values)
    {
        var ids = new List<Guid>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!Guid.TryParse(value, out var id)) return null;
            ids.Add(id);
        }
        return ids;
    }

    private static bool TryParseReviewFilter(string? status, out ReviewFilter review)
    {
        review = ReviewFilter.All;
        if (string.IsNullOrWhiteSpace(status)) return true;
        return Enum.TryParse(status, ignoreCase: true, out review);
    }
}

public sealed record UpdateImageTagsRequest(IReadOnlyList<TagRequest> Tags, bool MarkReviewed);
public sealed record TagRequest(string Type, string Value);
public sealed record MergeTagsRequest(Guid TargetTagId);
public sealed record TagView(Guid Id, string Type, string Value);
public sealed record ImageView(Guid Id, int Width, int Height, long Bytes, DateTimeOffset? CapturedAt, DateTimeOffset UploadedAt, DateTimeOffset? ReviewedAt, IReadOnlyList<TagView> Tags);
public sealed record ReviewResponse(int PendingCount, ImageView? Image);
