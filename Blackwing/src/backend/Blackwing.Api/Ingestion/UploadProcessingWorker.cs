using Blackwing.Api.Configuration;
using Blackwing.Persistence;
using Blackwing.Persistence.Gallery;
using Blackwing.Persistence.Ingestion;
using Blackwing.Shared.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Ingestion;

/// <summary>
/// The recoverable ingestion worker. It claims one pending <see cref="UploadJob"/> at
/// a time (atomically, so extra workers never collide), turns the staged bytes into a
/// pending-review <see cref="Image"/> with its stored derivatives, and records the
/// outcome per job. A failure isolates to its own job and never blocks the batch; a
/// crash mid-flight is healed on the next start by returning claimed jobs to the queue.
/// </summary>
public sealed class UploadProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IUploadStagingArea staging,
    IImageStore imageStore,
    ImageProcessingService processor,
    UploadSignal signal,
    IOptions<BlackwingOptions> options,
    ILogger<UploadProcessingWorker> logger) : BackgroundService
{
    private readonly IngestionOptions ingestion = options.Value.Ingestion;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverStuckJobsAsync(stoppingToken);

        var pollInterval = TimeSpan.FromSeconds(ingestion.PollSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            bool processedAny;
            try
            {
                processedAny = await DrainAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A failure claiming or draining is unexpected; log and back off rather
                // than spinning. Individual job failures are handled inside DrainAsync.
                logger.LogError(exception, "Upload worker drain failed; backing off.");
                processedAny = false;
            }

            if (!processedAny)
            {
                try { await signal.WaitAsync(pollInterval, stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }

    /// <summary>Returns jobs left in <c>Processing</c> by a previous crash to the queue.</summary>
    private async Task RecoverStuckJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();
            var now = DateTimeOffset.UtcNow;
            var recovered = await database.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE upload_jobs SET ""Status"" = 'Pending', ""UpdatedAt"" = {now} WHERE ""Status"" = 'Processing'",
                cancellationToken);
            if (recovered > 0) logger.LogInformation("Recovered {Count} interrupted upload job(s).", recovered);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to recover interrupted upload jobs.");
        }
    }

    /// <summary>Processes pending jobs until none remain; returns whether any were handled.</summary>
    private async Task<bool> DrainAsync(CancellationToken cancellationToken)
    {
        var handledAny = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<BlackwingDbContext>();

            var jobId = await ClaimNextAsync(database, cancellationToken);
            if (jobId is null) return handledAny;

            var job = await database.UploadJobs.FirstOrDefaultAsync(value => value.Id == jobId, cancellationToken);
            if (job is not null) await ProcessAsync(database, job, cancellationToken);
            handledAny = true;
        }

        return handledAny;
    }

    /// <summary>
    /// Atomically moves the oldest pending job to <c>Processing</c> and returns its id.
    /// <c>FOR UPDATE SKIP LOCKED</c> lets several workers run without ever picking the
    /// same job. Returns <c>null</c> when the queue is empty.
    /// </summary>
    private static async Task<Guid?> ClaimNextAsync(BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var claimed = await database.Database.SqlQueryRaw<Guid>(
            @"UPDATE upload_jobs SET ""Status"" = 'Processing', ""AttemptCount"" = ""AttemptCount"" + 1, ""UpdatedAt"" = {0}
              WHERE ""Id"" = (
                  SELECT ""Id"" FROM upload_jobs WHERE ""Status"" = 'Pending'
                  ORDER BY ""CreatedAt"" LIMIT 1 FOR UPDATE SKIP LOCKED
              )
              RETURNING ""Id"" AS ""Value""",
            now).ToListAsync(cancellationToken);
        return claimed.Count > 0 ? claimed[0] : null;
    }

    private async Task ProcessAsync(BlackwingDbContext database, UploadJob job, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        ProcessedImage processed;
        await using (var source = await staging.OpenReadAsync(job.OwnerUserId, job.StagingToken, cancellationToken))
        {
            if (source is null)
            {
                await FailAsync(database, job, UploadFailureCodes.StagingMissing, "The staged upload is no longer available.", now, discard: false, cancellationToken);
                return;
            }

            try
            {
                processed = processor.Process(source, cancellationToken);
            }
            catch (InvalidImageException exception)
            {
                await FailAsync(database, job, UploadFailureCodes.InvalidImage, exception.Message, now, discard: true, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Shutdown: leave the job Processing to be recovered on the next start.
            }
            catch (Exception exception)
            {
                await FailAsync(database, job, UploadFailureCodes.ProcessingError, exception.Message, now, discard: false, cancellationToken);
                return;
            }
        }

        try
        {
            await StoreDerivativesAsync(job, processed, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await FailAsync(database, job, UploadFailureCodes.StorageError, exception.Message, now, discard: false, cancellationToken);
            return;
        }

        await CompleteAsync(database, job, processed, now, cancellationToken);
    }

    private async Task StoreDerivativesAsync(UploadJob job, ProcessedImage processed, CancellationToken cancellationToken)
    {
        // Files are written before the row is inserted, so a crash here leaves harmless
        // orphan bytes rather than a record pointing at nothing. Re-writing identical,
        // content-addressed bytes on a retry is idempotent.
        await using (var original = await staging.OpenReadAsync(job.OwnerUserId, job.StagingToken, cancellationToken))
        {
            if (original is null) throw new InvalidOperationException("The staged original disappeared before it could be stored.");
            await imageStore.SaveAsync(job.OwnerUserId, job.Sha256, ImageDerivative.Original, original, cancellationToken);
        }

        await using (var preview = new MemoryStream(processed.Preview, writable: false))
            await imageStore.SaveAsync(job.OwnerUserId, job.Sha256, ImageDerivative.Preview, preview, cancellationToken);
        await using (var thumbnail = new MemoryStream(processed.Thumbnail, writable: false))
            await imageStore.SaveAsync(job.OwnerUserId, job.Sha256, ImageDerivative.Thumbnail, thumbnail, cancellationToken);
    }

    private async Task CompleteAsync(BlackwingDbContext database, UploadJob job, ProcessedImage processed, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // A duplicate can only appear here if the same bytes were staged twice (a batch
        // with the file repeated, or a re-upload while a job was queued): the endpoint
        // dedups against existing images, and the unique index is the final guard.
        if (await database.Images.AnyAsync(image => image.OwnerUserId == job.OwnerUserId && image.Sha256 == job.Sha256, cancellationToken))
        {
            job.MarkDuplicate(now);
            await database.SaveChangesAsync(cancellationToken);
            await staging.DiscardAsync(job.OwnerUserId, job.StagingToken, cancellationToken);
            return;
        }

        var contentType = ImageFormatDetector.ContentType(processed.Format);
        var image = Image.Create(new ImageValues(job.Sha256, contentType, processed.Width, processed.Height, job.Bytes, processed.CapturedAt), job.OwnerUserId, now);
        database.Images.Add(image);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race to another writer for the same bytes; fall back to duplicate.
            database.Entry(image).State = EntityState.Detached;
            if (!await database.Images.AnyAsync(existing => existing.OwnerUserId == job.OwnerUserId && existing.Sha256 == job.Sha256, cancellationToken))
                throw;
            job.MarkDuplicate(now);
            await database.SaveChangesAsync(cancellationToken);
            await staging.DiscardAsync(job.OwnerUserId, job.StagingToken, cancellationToken);
            return;
        }

        job.Complete(image.Id, now);
        await database.SaveChangesAsync(cancellationToken);
        await staging.DiscardAsync(job.OwnerUserId, job.StagingToken, cancellationToken);
        logger.LogInformation("Ingested upload {JobId} as image {ImageId} for owner {OwnerId}.", job.Id, image.Id, job.OwnerUserId);
    }

    private async Task FailAsync(BlackwingDbContext database, UploadJob job, string code, string message, DateTimeOffset now, bool discard, CancellationToken cancellationToken)
    {
        job.Fail(code, message, now);
        await database.SaveChangesAsync(cancellationToken);
        // Recoverable failures keep the staged bytes so a retry can reuse them; a
        // permanent failure discards them because a retry cannot help.
        if (discard || !UploadFailureCodes.IsRecoverable(code))
            await staging.DiscardAsync(job.OwnerUserId, job.StagingToken, cancellationToken);
        logger.LogWarning("Upload {JobId} failed ({Code}) for owner {OwnerId}: {Message}", job.Id, code, job.OwnerUserId, message);
    }
}
