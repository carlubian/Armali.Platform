using System.Security.Claims;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Platform.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;

namespace Segaris.Api.Platform.Attachments;

internal static class AttachmentProbeEndpoints
{
    private static readonly AttachmentOwner Owner = new("Platform", "Probe", "shared");

    public static void MapAttachmentProbes(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("platform/attachments", "Attachment probes")
            .RequireAuthorization();

        group.MapPost("", UploadAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithRequestBodyLimit(AttachmentPolicy.MaximumFileSize + (1024 * 1024))
            .WithSummary("Uploads an attachment for integration testing");
        group.MapGet("/{id:int}", DownloadAsync)
            .WithSummary("Downloads an attachment for integration testing");
        group.MapGet("/{id:int}/metadata", MetadataAsync)
            .WithSummary("Returns attachment metadata for integration testing");
        group.MapDelete("/{id:int}", DeleteAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Deletes an attachment for integration testing");
        group.MapGet("/reconciliation", ReconciliationAsync)
            .WithSummary("Inspects attachment storage consistency for integration testing");
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            throw AttachmentProblem.Invalid("file", "A multipart form file is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            throw AttachmentProblem.Invalid("file", "A multipart form file is required.");
        }

        await using var stream = file.OpenReadStream();
        var userId = int.Parse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier)!,
            System.Globalization.CultureInfo.InvariantCulture);
        var descriptor = await attachments.CreateAsync(
            new(Owner, file.FileName, file.ContentType, stream),
            new UserId(userId),
            cancellationToken);
        return TypedResults.Created($"/api/platform/attachments/{descriptor.Id.Value}", descriptor);
    }

    private static async Task<IResult> MetadataAsync(
        int id,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var descriptor = await attachments.FindAsync(new(id), Owner, cancellationToken);
        return descriptor is null ? throw ApiProblemException.NotFound() : TypedResults.Ok(descriptor);
    }

    private static async Task<IResult> DownloadAsync(
        int id,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        var download = await attachments.OpenReadAsync(new(id), Owner, cancellationToken);
        if (download is null)
        {
            throw ApiProblemException.NotFound();
        }

        return Results.Stream(
            download.Content,
            download.Descriptor.ContentType,
            download.Descriptor.FileName,
            enableRangeProcessing: false);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        IAttachmentService attachments,
        CancellationToken cancellationToken)
    {
        return await attachments.DeleteAsync(new(id), Owner, cancellationToken)
            ? TypedResults.NoContent()
            : throw ApiProblemException.NotFound();
    }

    private static async Task<IResult> ReconciliationAsync(
        AttachmentReconciliationService reconciliation,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await reconciliation.InspectAsync(cancellationToken));
}
