using Blackwing.Api.Configuration;
using Blackwing.Persistence;
using Blackwing.Persistence.Ingestion;
using Blackwing.Shared.Ownership;
using Blackwing.Shared.Storage;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Blackwing.Api.Ingestion;

public static class UploadEndpoints
{
    /// <summary>Client-facing reasons an individual file was rejected at upload time.</summary>
    public const string ReasonTooLarge = "too_large";
    public const string ReasonUnsupportedFormat = "unsupported_format";

    private const int RecentJobLimit = 200;

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/images/uploads").RequireAuthorization();
        group.MapPost("/", ReceiveUpload);
        group.MapGet("/", ListJobs);
        group.MapPost("/{id:guid}/retry", RetryJob);
        return endpoints;
    }

    /// <summary>
    /// Streams a multipart batch of files into the staging area — one at a time, never
    /// buffering a whole file — validating format and size, rejecting per-user
    /// duplicates, and queuing a job per accepted file. Returns immediately with a
    /// per-file result so a large import does not block the caller.
    /// </summary>
    private static async Task<IResult> ReceiveUpload(
        HttpContext context,
        IUserScope userScope,
        BlackwingDbContext database,
        IUploadStagingArea staging,
        UploadSignal signal,
        IOptions<BlackwingOptions> options,
        CancellationToken cancellationToken)
    {
        if (!IsMultipart(context.Request.ContentType, out var boundary))
            return Results.BadRequest(new { error = "Expected a multipart/form-data upload." });

        // The per-file limit is enforced while streaming; lift the total-body cap so a
        // legitimate large batch is not cut off by Kestrel's default.
        var bodySize = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (bodySize is not null && !bodySize.IsReadOnly) bodySize.MaxRequestBodySize = null;

        var owner = userScope.UserId;
        var maxBytes = options.Value.Ingestion.MaxFileBytes;
        var results = new List<UploadFileResult>();
        var accepted = false;

        var reader = new MultipartReader(boundary, context.Request.Body);
        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync(cancellationToken)) is not null)
        {
            var disposition = section.GetContentDispositionHeader();
            if (disposition is null || !disposition.IsFileDisposition()) continue;
            var fileName = disposition.FileName.HasValue ? disposition.FileName.Value! : "upload";

            var staged = await staging.StageAsync(owner, section.Body, maxBytes, cancellationToken);
            if (staged.Outcome != StagingOutcome.Staged)
            {
                results.Add(UploadFileResult.Rejected(fileName, ReasonTooLarge));
                continue;
            }

            var format = ImageFormatDetector.Detect(staged.Header);
            if (format is null)
            {
                await staging.DiscardAsync(owner, staged.Token, cancellationToken);
                results.Add(UploadFileResult.Rejected(fileName, ReasonUnsupportedFormat));
                continue;
            }

            if (await database.Images.AnyAsync(image => image.OwnerUserId == owner && image.Sha256 == staged.Sha256, cancellationToken))
            {
                await staging.DiscardAsync(owner, staged.Token, cancellationToken);
                results.Add(UploadFileResult.Duplicate(fileName));
                continue;
            }

            var job = UploadJob.Create(owner, fileName, ImageFormatDetector.ContentType(format.Value), staged.Bytes, staged.Sha256, staged.Token, DateTimeOffset.UtcNow);
            database.UploadJobs.Add(job);
            await database.SaveChangesAsync(cancellationToken);
            accepted = true;
            results.Add(UploadFileResult.Accepted(fileName, job.Id));
        }

        if (accepted) signal.Notify();
        return Results.Ok(new UploadBatchResponse(results));
    }

    /// <summary>Returns the owner's most recent jobs so the UI can track ingestion progress.</summary>
    private static async Task<IResult> ListJobs(IUserScope userScope, BlackwingDbContext database, CancellationToken cancellationToken)
    {
        var owner = userScope.UserId;
        var jobs = await database.UploadJobs
            .Where(job => job.OwnerUserId == owner)
            .OrderByDescending(job => job.CreatedAt)
            .Take(RecentJobLimit)
            .Select(job => new UploadJobView(
                job.Id,
                job.OriginalFileName,
                job.Status.ToString(),
                job.ImageId,
                job.FailureCode,
                job.FailureCode != null && UploadFailureCodes.IsRecoverable(job.FailureCode),
                job.Bytes,
                job.CreatedAt,
                job.UpdatedAt))
            .ToListAsync(cancellationToken);
        return Results.Ok(new UploadJobListResponse(jobs));
    }

    /// <summary>Requeues one of the owner's recoverable failed jobs for another attempt.</summary>
    private static async Task<IResult> RetryJob(Guid id, IUserScope userScope, BlackwingDbContext database, UploadSignal signal, CancellationToken cancellationToken)
    {
        var owner = userScope.UserId;
        var job = await database.UploadJobs.FirstOrDefaultAsync(value => value.Id == id && value.OwnerUserId == owner, cancellationToken);
        if (job is null) return Results.NotFound();
        if (job.Status != UploadJobStatus.Failed || !UploadFailureCodes.IsRecoverable(job.FailureCode))
            return Results.Conflict(new { error = "Only a recoverable failed upload can be retried." });

        job.Requeue(DateTimeOffset.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        signal.Notify();
        return Results.NoContent();
    }

    private static bool IsMultipart(string? contentType, out string boundary)
    {
        boundary = string.Empty;
        if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType)) return false;
        var value = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
        if (string.IsNullOrWhiteSpace(value)) return false;
        boundary = value;
        return true;
    }
}

/// <summary>The outcome for one file in an upload batch. Status is accepted, duplicate or rejected.</summary>
public sealed record UploadFileResult(string FileName, string Status, Guid? JobId, string? Reason)
{
    public static UploadFileResult Accepted(string fileName, Guid jobId) => new(fileName, "accepted", jobId, null);
    public static UploadFileResult Duplicate(string fileName) => new(fileName, "duplicate", null, null);
    public static UploadFileResult Rejected(string fileName, string reason) => new(fileName, "rejected", null, reason);
}

public sealed record UploadBatchResponse(IReadOnlyList<UploadFileResult> Files);

public sealed record UploadJobView(
    Guid Id,
    string FileName,
    string Status,
    Guid? ImageId,
    string? FailureCode,
    bool Recoverable,
    long Bytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UploadJobListResponse(IReadOnlyList<UploadJobView> Jobs);
