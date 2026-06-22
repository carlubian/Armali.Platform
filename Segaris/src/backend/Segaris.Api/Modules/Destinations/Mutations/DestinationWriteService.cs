using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Destinations.Mutations;

/// <summary>
/// Write-side operations for destinations. Public destinations are collaboratively
/// mutable; private destinations remain creator-only and only creators can change
/// visibility.
/// </summary>
internal sealed class DestinationWriteService(SegarisDbContext database, IAttachmentService attachments, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateDestinationRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = MapCreate(request);
        var destination = Destination.Create(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        database.Add(destination);
        await database.SaveChangesAsync(cancellationToken);
        return destination.Id;
    }

    public async Task<bool> UpdateAsync(
        int destinationId,
        UpdateDestinationRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var destination = await database.Set<Destination>()
            .Where(DestinationPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == destinationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (destination is null)
        {
            return false;
        }

        var values = MapUpdate(request);
        destination.Update(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int destinationId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var destination = await database.Set<Destination>()
            .Where(DestinationPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == destinationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (destination is null)
        {
            return false;
        }

        database.Remove(destination);
        await database.SaveChangesAsync(cancellationToken);
        await DeleteAttachmentFilesAsync(destinationId, cancellationToken);
        return true;
    }

    public async Task<DestinationSetPrimaryResult> SetPrimaryAttachmentAsync(
        int destinationId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var destination = await database.Set<Destination>()
            .Where(DestinationPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == destinationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (destination is null)
        {
            return new(DestinationSetPrimaryOutcome.DestinationNotFound, null);
        }

        var owner = DestinationsAttachments.DestinationOwner(destinationId);
        var descriptor = await attachments.FindAsync(new(attachmentId), owner, cancellationToken);
        if (descriptor is null)
        {
            return new(DestinationSetPrimaryOutcome.AttachmentNotFound, null);
        }

        if (!DestinationsAttachments.IsImageContentType(descriptor.ContentType))
        {
            return new(DestinationSetPrimaryOutcome.NotImage, null);
        }

        destination.SetPrimaryAttachment(attachmentId, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return new(DestinationSetPrimaryOutcome.Assigned, descriptor);
    }

    public async Task<DestinationDeleteAttachmentOutcome> DeleteAttachmentAsync(
        int destinationId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var destination = await database.Set<Destination>()
            .Where(DestinationPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == destinationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (destination is null)
        {
            return DestinationDeleteAttachmentOutcome.DestinationNotFound;
        }

        var owner = DestinationsAttachments.DestinationOwner(destinationId);
        var removed = await attachments.DeleteAsync(new(attachmentId), owner, cancellationToken);
        if (!removed)
        {
            return DestinationDeleteAttachmentOutcome.AttachmentNotFound;
        }

        destination.ClearPrimaryAttachmentIf(attachmentId, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return DestinationDeleteAttachmentOutcome.Deleted;
    }

    private static DestinationValues MapCreate(CreateDestinationRequest request) =>
        new(
            request.Name ?? string.Empty,
            request.CategoryId,
            request.Country,
            request.EntryRequirements,
            request.IsSchengenArea,
            request.Notes,
            ParseVisibility(request.Visibility));

    private static DestinationValues MapUpdate(UpdateDestinationRequest request) =>
        new(
            request.Name ?? string.Empty,
            request.CategoryId,
            request.Country,
            request.EntryRequirements,
            request.IsSchengenArea,
            request.Notes,
            ParseVisibility(request.Visibility));

    private async Task ValidateCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await database.Set<DestinationCategory>()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);
        if (!exists)
        {
            throw new DestinationsValidationException(
                "The selected destination category does not exist.",
                DestinationsValidationReason.CatalogReference);
        }
    }

    private async Task DeleteAttachmentFilesAsync(int destinationId, CancellationToken cancellationToken)
    {
        var owner = DestinationsAttachments.DestinationOwner(destinationId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }
    }

    private static RecordVisibility ParseVisibility(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DestinationsDefaults.Visibility;
        }

        if (Enum.TryParse<RecordVisibility>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new DestinationsValidationException("The visibility is not a recognized value.");
    }
}
