using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Opex.Contracts;
using Segaris.Api.Modules.Opex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Opex.Mutations;

/// <summary>
/// Write-side operations for Opex contracts: create, full replacement update, and
/// physical deletion. Each mutation runs in a single <c>SaveChangesAsync</c>
/// transaction and validates catalog references and global normalized-name
/// uniqueness before the row is persisted. Authorization mirrors the read side: a
/// public contract is mutable by any user (collaboration) while a private contract
/// is mutable only by its creator, and only the creator may change visibility.
/// Deletion cascades to occurrences at the database level and reconciles contract
/// attachments through the platform service afterwards.
/// </summary>
internal sealed class OpexContractWriteService(
    SegarisDbContext database,
    OpexCatalogValidator catalogValidator,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int> CreateAsync(
        CreateOpexContractRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.MovementType,
            request.Status,
            request.StartDate,
            request.ClosedDate,
            request.EstimatedAnnualAmount,
            request.ExpectedFrequency,
            request.CategoryId,
            request.SupplierId,
            request.CostCenterId,
            request.CurrencyId,
            request.Notes,
            request.Visibility);

        // Shape, enum, and amount validation happen in the domain factory; catalog
        // references and global-name uniqueness are checked before the row is saved.
        var contract = OpexContract.Create(values, actorId, clock.UtcNow);
        await catalogValidator.ValidateAsync(values, cancellationToken);
        await EnsureUniqueNameAsync(contract.NormalizedName, excludeId: null, cancellationToken);

        database.Add(contract);
        await SaveAsync(cancellationToken);
        return contract.Id;
    }

    public async Task<bool> UpdateAsync(
        int contractId,
        UpdateOpexContractRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contract = await database.Set<OpexContract>()
            .Where(OpexContractPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == contractId)
            .FirstOrDefaultAsync(cancellationToken);
        if (contract is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.MovementType,
            request.Status,
            request.StartDate,
            request.ClosedDate,
            request.EstimatedAnnualAmount,
            request.ExpectedFrequency,
            request.CategoryId,
            request.SupplierId,
            request.CostCenterId,
            request.CurrencyId,
            request.Notes,
            request.Visibility);

        // The domain applies shape validation and the creator-only visibility policy;
        // catalog references and global-name uniqueness are validated before the
        // single transactional save (last write wins, no concurrency token).
        contract.Update(values, actorId, clock.UtcNow);
        await catalogValidator.ValidateAsync(values, cancellationToken);
        await EnsureUniqueNameAsync(contract.NormalizedName, excludeId: contractId, cancellationToken);

        await SaveAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int contractId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var contract = await database.Set<OpexContract>()
            .Where(OpexContractPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == contractId)
            .FirstOrDefaultAsync(cancellationToken);
        if (contract is null)
        {
            return false;
        }

        // The occurrence identifiers are captured before deletion so their files can
        // be reconciled afterwards: the database cascade removes the occurrence rows
        // but not their out-of-transaction attachments.
        var occurrenceIds = await database.Set<OpexOccurrence>()
            .AsNoTracking()
            .Where(occurrence => occurrence.ContractId == contractId)
            .Select(occurrence => occurrence.Id)
            .ToArrayAsync(cancellationToken);

        // Deletion is physical and cascades to occurrences at the database level.
        database.Remove(contract);
        await database.SaveChangesAsync(cancellationToken);

        // Compensating storage cleanup runs after the contract row is gone. Files are
        // outside the database transaction, so any residue is reconciled later rather
        // than resurrecting the deleted contract. Both the contract-level attachments
        // and every cascaded occurrence's attachments are removed here.
        await DeleteAttachmentsAsync(OpexAttachments.ContractOwner(contractId), cancellationToken);
        foreach (var occurrenceId in occurrenceIds)
        {
            await DeleteAttachmentsAsync(OpexAttachments.OccurrenceOwner(occurrenceId), cancellationToken);
        }

        return true;
    }

    private async Task DeleteAttachmentsAsync(AttachmentOwner owner, CancellationToken cancellationToken)
    {
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }
    }

    private async Task EnsureUniqueNameAsync(string normalizedName, int? excludeId, CancellationToken cancellationToken)
    {
        var conflict = await database.Set<OpexContract>()
            .AsNoTracking()
            .AnyAsync(
                contract => contract.NormalizedName == normalizedName && contract.Id != excludeId,
                cancellationToken);
        if (conflict)
        {
            throw new OpexValidationException(
                "A contract with the same name already exists.",
                OpexValidationReason.DuplicateName);
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The normalized-name unique index is the only constraint reachable after
            // catalog references and uniqueness are pre-validated; surface it as the
            // same duplicate-name conflict if a concurrent writer won the race.
            throw new OpexValidationException(
                "A contract with the same name already exists.",
                OpexValidationReason.DuplicateName);
        }
    }

    private static OpexContractValues Map(
        string? name,
        string? movementType,
        string? status,
        DateOnly? startDate,
        DateOnly? closedDate,
        decimal? estimatedAnnualAmount,
        string? expectedFrequency,
        int categoryId,
        int? supplierId,
        int? costCenterId,
        int currencyId,
        string? notes,
        string? visibility) =>
        new(
            name ?? string.Empty,
            ParseEnum<OpexMovementType>(movementType, "movement type"),
            ParseEnum<OpexContractStatus>(status, "status"),
            startDate,
            closedDate,
            estimatedAnnualAmount,
            ParseEnum<OpexExpectedFrequency>(expectedFrequency, "expected frequency"),
            categoryId,
            supplierId,
            costCenterId,
            currencyId,
            notes,
            ParseEnum<RecordVisibility>(visibility, "visibility"));

    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new OpexValidationException($"The {field} is not a recognized value.");
        }

        return parsed;
    }
}
