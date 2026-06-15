using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Opex.Mutations;

/// <summary>
/// Write-side operations for the occurrences subordinate to a contract: create,
/// full replacement update, and physical deletion. Authorization is always
/// resolved through the parent contract by the caller before these run; every
/// mutation is additionally scoped to the supplied <c>contractId</c> so an
/// occurrence identifier from another contract can never be addressed. Occurrences
/// carry no movement type, currency, classification, or visibility of their own and
/// inherit everything from the parent. Deletion is physical and reconciles the
/// occurrence's own attachments through the platform service afterwards.
/// </summary>
internal sealed class OpexOccurrenceWriteService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int> CreateAsync(
        int contractId,
        CreateOpexOccurrenceRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Shape, amount, and date validation happen in the domain factory; the
        // parent contract's existence and accessibility are checked by the caller.
        var values = Map(request.EffectiveDate, request.ActualAmount, request.Description, request.Notes);
        var occurrence = OpexOccurrence.Create(contractId, values, actorId, clock.UtcNow);

        database.Add(occurrence);
        await database.SaveChangesAsync(cancellationToken);
        return occurrence.Id;
    }

    public async Task<bool> UpdateAsync(
        int contractId,
        int occurrenceId,
        UpdateOpexOccurrenceRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var occurrence = await database.Set<OpexOccurrence>()
            .Where(candidate => candidate.ContractId == contractId && candidate.Id == occurrenceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (occurrence is null)
        {
            return false;
        }

        // Last write wins; there is no concurrency token. The single transactional
        // save makes the refreshed contract realized total visible immediately.
        var values = Map(request.EffectiveDate, request.ActualAmount, request.Description, request.Notes);
        occurrence.Update(values, actorId, clock.UtcNow);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int contractId,
        int occurrenceId,
        CancellationToken cancellationToken)
    {
        var occurrence = await database.Set<OpexOccurrence>()
            .Where(candidate => candidate.ContractId == contractId && candidate.Id == occurrenceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (occurrence is null)
        {
            return false;
        }

        database.Remove(occurrence);
        await database.SaveChangesAsync(cancellationToken);

        // Compensating storage cleanup runs after the occurrence row is gone. Files
        // are outside the database transaction, so any residue is reconciled later
        // rather than resurrecting the deleted occurrence.
        var owner = OpexAttachments.OccurrenceOwner(occurrenceId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    private static OpexOccurrenceValues Map(
        DateOnly? effectiveDate,
        decimal actualAmount,
        string? description,
        string? notes)
    {
        if (effectiveDate is not { } date)
        {
            throw new OpexValidationException("The effective date is required.");
        }

        return new OpexOccurrenceValues(date, actualAmount, description, notes);
    }
}
