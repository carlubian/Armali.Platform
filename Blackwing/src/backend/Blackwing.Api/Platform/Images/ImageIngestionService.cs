using Blackwing.Api.Configuration;
using Blackwing.Api.Platform.Api;
using Blackwing.Persistence;
using Blackwing.Persistence.Content;
using Blackwing.Shared.Content;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Images;
using Blackwing.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Blackwing.Api.Platform.Images;

/// <summary>The outcome of ingesting one file.</summary>
/// <param name="ImageId">The image row: the new one, or the one that was already there.</param>
/// <param name="WasDuplicate">
/// The account already held exactly these bytes, so nothing was created.
/// </param>
internal sealed record ImageIngestionResult(int ImageId, bool WasDuplicate);

/// <summary>
/// Takes one uploaded file from raw bytes to a stored, recorded image. This is the path the
/// synchronous endpoint uses today and the one the phase 4 background worker will call; it does
/// not know which of them is asking.
/// </summary>
internal sealed class ImageIngestionService(
    IImageBlobStore blobStore,
    IImageProcessor processor,
    BlackwingDbContext database,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<ImageOptions> options,
    ILogger<ImageIngestionService> logger)
{
    /// <summary>The unique index that rejects an exact duplicate per account.</summary>
    private const string DuplicateIndexName = "IX_images_owner_content_hash";

    public async Task<ImageIngestionResult> IngestAsync(
        Stream content,
        string fileName,
        string? declaredContentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var owner = currentUser.UserId
            ?? throw new InvalidOperationException("An image cannot be ingested without an authenticated user.");

        var normalizedName = ImagePolicy.NormalizeAndValidateFileName(fileName);
        var contentType = ImagePolicy.ResolveContentType(normalizedName);
        ImagePolicy.EnsureDeclaredContentTypeMatches(normalizedName, declaredContentType);

        // Staging cleans up after itself when it fails, so nothing is left to discard if it throws.
        var staged = await blobStore.StageAsync(
            content,
            options.Value.MaximumFileSizeBytes,
            cancellationToken);

        var promoted = false;
        try
        {
            ProcessedImage? processed = null;
            DateTimeOffset? capturedAt = null;
            int? existingId;

            // The staged file must be closed before it is moved or discarded: an open handle
            // would block both on Windows and is untidy everywhere else.
            await using (var stream = await blobStore.OpenStagedAsync(staged, cancellationToken))
            {
                ImagePolicy.ValidateContent(normalizedName, stream);

                // The global filter has already narrowed this to the current account, so no owner
                // predicate is written here.
                existingId = await database.Images
                    .Where(image => image.ContentHash == staged.ContentHash)
                    .Select(image => (int?)image.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingId is null)
                {
                    capturedAt = ImageMetadataReader.ReadCapturedAt(stream);
                    processed = processor.Process(stream);
                }
            }

            if (existingId is { } duplicateId)
            {
                await blobStore.DiscardStagedAsync(staged, cancellationToken);
                return new ImageIngestionResult(duplicateId, WasDuplicate: true);
            }

            await blobStore.PromoteAsync(owner, staged, processed!.Derivatives, cancellationToken);
            promoted = true;

            // The contract takes no owner. The context stamps it from the request identity.
            var image = new Image
            {
                ContentHash = staged.ContentHash,
                OriginalFileName = normalizedName,
                ContentType = contentType,
                ByteSize = staged.ByteSize,
                Width = processed.Width,
                Height = processed.Height,
                CapturedAt = capturedAt,
                UploadedAt = clock.UtcNow,
                ReviewState = ImageReviewState.Pending,
            };

            database.Images.Add(image);
            await database.SaveChangesAsync(cancellationToken);

            return new ImageIngestionResult(image.Id, WasDuplicate: false);
        }
        catch (DbUpdateException exception) when (IsDuplicateRace(exception))
        {
            // Another upload of the same file won the race between the check above and the
            // insert. Its row now owns the files on disk, which are content-addressed and
            // therefore the very ones this attempt just wrote, so nothing is removed.
            database.ChangeTracker.Clear();
            throw DuplicateProblem();
        }
        catch
        {
            database.ChangeTracker.Clear();
            await CleanUpAsync(owner, staged, promoted);
            throw;
        }
    }

    private static bool IsDuplicateRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DuplicateIndexName,
        };

    internal static ApiProblemException DuplicateProblem() => new(
        StatusCodes.Status409Conflict,
        ImageErrorCodes.Duplicate,
        "The image is already in the library.");

    /// <summary>
    /// Removes whatever this attempt left on disk. It runs while another exception is already in
    /// flight, so a failure here is logged rather than allowed to replace the original error.
    /// </summary>
    private async Task CleanUpAsync(UserId owner, StagedImage staged, bool promoted)
    {
        try
        {
            if (promoted)
            {
                await blobStore.DeleteAsync(owner, staged.ContentHash, CancellationToken.None);
            }
            else
            {
                await blobStore.DiscardStagedAsync(staged, CancellationToken.None);
            }
        }
        catch (ApiProblemException exception)
        {
            logger.LogWarning(exception, "Cleaning up after a failed image ingestion did not complete.");
        }
    }
}
