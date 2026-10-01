using Blackwing.Api.Configuration;
using Blackwing.Api.Modules.Identity.Security;
using Blackwing.Api.Platform.Api;
using Blackwing.Persistence;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Images;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Blackwing.Api.Platform.Images.Endpoints;

/// <summary>
/// Upload, metadata, serving and deletion of the current account's images.
/// </summary>
/// <remarks>
/// Another account's image is filtered out of every query by the global ownership filter, so it
/// answers 404 and never 403: a 403 would confirm that the identifier exists, which is itself a
/// leak. Image files are never served as static files; every byte goes through
/// <see cref="IImageBlobStore"/> after the row has been found for the current account.
/// </remarks>
internal static class ImageEndpoints
{
    /// <summary>
    /// Room, on top of the maximum file size, for the multipart framing around the file part.
    /// </summary>
    private const long MultipartOverheadBytes = 1024 * 1024;

    /// <summary>One year, the conventional ceiling: the content behind a hash never changes.</summary>
    private const string ImmutableCacheControl = "private, immutable, max-age=31536000";

    public static void MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBlackwingApiGroup("images", "Images")
            .RequireAuthorization();

        group.MapPost("", UploadAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Uploads one image as multipart/form-data and stores it synchronously");

        group.MapGet("/{id:int}", GetAsync)
            .WithSummary("Returns the metadata of one of the current user's images");

        MapServe(group, "/{id:int}/thumb", ImageVariant.Thumbnail, "Streams the thumbnail of one of the current user's images");
        MapServe(group, "/{id:int}/preview", ImageVariant.Preview, "Streams the preview of one of the current user's images");
        MapServe(group, "/{id:int}/original", ImageVariant.Original, "Streams the untouched original of one of the current user's images");

        group.MapDelete("/{id:int}", DeleteAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Deletes one of the current user's images, its tag links and its files");
    }

    private static void MapServe(RouteGroupBuilder group, string route, ImageVariant variant, string summary) =>
        group.MapGet(
                route,
                (int id,
                    HttpContext context,
                    BlackwingDbContext database,
                    IImageBlobStore store,
                    ICurrentUser currentUser,
                    ILoggerFactory loggerFactory,
                    CancellationToken cancellationToken) =>
                    ServeAsync(id, variant, context, database, store, currentUser, loggerFactory, cancellationToken))
            // A renewed session cookie would overwrite the cache headers set below, so these
            // routes opt out of the renewal. The session is still validated on every request.
            .WithMetadata(SkipSessionRenewalMetadata.Instance)
            .WithSummary(summary);

    private static async Task<IResult> UploadAsync(
        HttpContext context,
        ImageIngestionService ingestion,
        BlackwingDbContext database,
        IOptions<ImageOptions> options,
        CancellationToken cancellationToken)
    {
        var request = context.Request;
        var maximumBody = options.Value.MaximumFileSizeBytes + MultipartOverheadBytes;

        // The limit has to be in place before the first byte of the body is read. Kestrel applies
        // it while reading, so an oversized request is cut off instead of being received whole;
        // a declared length lets us refuse without reading anything at all.
        if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } sizeLimit)
        {
            sizeLimit.MaxRequestBodySize = maximumBody;
        }

        if (request.ContentLength > maximumBody)
        {
            throw TooLarge(options.Value.MaximumFileSizeBytes);
        }

        var boundary = ReadBoundary(request);

        // The body is streamed part by part rather than bound to an IFormFile, which would copy
        // the whole upload to a temporary file before the endpoint ever saw it. Only the first
        // file part is used; anything after it is not read.
        var reader = new MultipartReader(boundary, request.Body);
        ImageIngestionResult? result = null;

        MultipartSection? section;
        while ((section = await ReadSectionAsync(reader, cancellationToken)) is not null)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
                || !disposition.IsFileDisposition())
            {
                continue;
            }

            var fileName = disposition.FileNameStar.HasValue
                ? disposition.FileNameStar.Value
                : HeaderUtilities.RemoveQuotes(disposition.FileName).Value;

            result = await ingestion.IngestAsync(
                section.Body,
                fileName ?? string.Empty,
                section.ContentType,
                cancellationToken);
            break;
        }

        if (result is null)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["file"] = ["The request must contain one file part."],
                });
        }

        if (result.WasDuplicate)
        {
            throw ImageIngestionService.DuplicateProblem();
        }

        var created = await FindDescriptorAsync(database, result.ImageId, cancellationToken)
            ?? throw ApiProblemException.NotFound();

        return TypedResults.Created($"/api/images/{created.Id}", created);
    }

    private static async Task<IResult> GetAsync(
        int id,
        BlackwingDbContext database,
        CancellationToken cancellationToken)
    {
        var descriptor = await FindDescriptorAsync(database, id, cancellationToken);
        return descriptor is null ? throw ApiProblemException.NotFound() : TypedResults.Ok(descriptor);
    }

    private static async Task<IResult> ServeAsync(
        int id,
        ImageVariant variant,
        HttpContext context,
        BlackwingDbContext database,
        IImageBlobStore store,
        ICurrentUser currentUser,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // The row comes first, and it is the ownership check: another account's image is not in
        // the result, so there is nothing to confirm and nothing to open.
        var image = await database.Images
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new { candidate.ContentHash, candidate.OriginalFileName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw ApiProblemException.NotFound();

        var owner = currentUser.UserId
            ?? throw new InvalidOperationException("An authenticated request has no user identifier.");

        var blob = await store.OpenReadAsync(owner, image.ContentHash, variant, cancellationToken);
        if (blob is null)
        {
            // The row exists but its file does not: the volume and the database disagree. That is
            // a server-side inconsistency to investigate, not a missing resource.
            loggerFactory.CreateLogger("Blackwing.Api.Platform.Images.ImageEndpoints")
                .LogError("Image {ImageId} has no stored {Variant} file.", id, variant);

            throw new ApiProblemException(
                StatusCodes.Status503ServiceUnavailable,
                ImageErrorCodes.StorageUnavailable,
                "The image storage is unavailable.");
        }

        context.Response.Headers.CacheControl = ImmutableCacheControl;

        // The ETag is strong and derived from the hash, so it is correct by construction: if the
        // bytes changed it would be a different image with a different identifier. The overload
        // answers If-None-Match with a 304 by itself.
        return Results.Stream(
            blob.Content,
            contentType: blob.ContentType,
            fileDownloadName: variant == ImageVariant.Original ? image.OriginalFileName : null,
            entityTag: new EntityTagHeaderValue($"\"{image.ContentHash}-{VariantName(variant)}\""),
            enableRangeProcessing: variant == ImageVariant.Original);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        BlackwingDbContext database,
        IImageBlobStore store,
        ICurrentUser currentUser,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var image = await database.Images
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiProblemException.NotFound();

        var owner = currentUser.UserId
            ?? throw new InvalidOperationException("An authenticated request has no user identifier.");
        var contentHash = image.ContentHash;

        // The row goes first and its tag links with it, through the cascade. Doing it the other
        // way round would let a failure leave a live row pointing at a file that is gone, which
        // is worse than a file nobody points at.
        database.Images.Remove(image);
        await database.SaveChangesAsync(cancellationToken);

        try
        {
            await store.DeleteAsync(owner, contentHash, cancellationToken);
        }
        catch (ApiProblemException exception)
        {
            loggerFactory.CreateLogger("Blackwing.Api.Platform.Images.ImageEndpoints")
                .LogWarning(exception, "Image {ImageId} was deleted but its files could not be removed.", id);
        }

        return TypedResults.NoContent();
    }

    private static Task<ImageResponse?> FindDescriptorAsync(
        BlackwingDbContext database,
        int id,
        CancellationToken cancellationToken) =>
        database.Images
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new ImageResponse(
                candidate.Id,
                candidate.OriginalFileName,
                candidate.ContentType,
                candidate.ByteSize,
                candidate.Width,
                candidate.Height,
                candidate.CapturedAt,
                candidate.UploadedAt,
                candidate.SortedAt,
                candidate.ReviewState.ToString().ToLowerInvariant()))
            .FirstOrDefaultAsync(cancellationToken);

    private static string ReadBoundary(HttpRequest request)
    {
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            || !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || StringSegment.IsNullOrEmpty(mediaType.Boundary))
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "The request must be multipart/form-data.");
        }

        return HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value!;
    }

    private static async Task<MultipartSection?> ReadSectionAsync(
        MultipartReader reader,
        CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadNextSectionAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "The multipart body is malformed.");
        }
    }

    private static ApiProblemException TooLarge(long maximumBytes) => new(
        StatusCodes.Status413PayloadTooLarge,
        ImageErrorCodes.TooLarge,
        "The image is too large.",
        $"Images may not exceed {maximumBytes} bytes.");

    private static string VariantName(ImageVariant variant) => variant switch
    {
        ImageVariant.Original => "original",
        ImageVariant.Thumbnail => "thumb",
        ImageVariant.Preview => "preview",
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown image variant."),
    };

    internal sealed record ImageResponse(
        int Id,
        string OriginalFileName,
        string ContentType,
        long ByteSize,
        int Width,
        int Height,
        DateTimeOffset? CapturedAt,
        DateTimeOffset UploadedAt,
        DateTimeOffset SortedAt,
        string ReviewState);
}
