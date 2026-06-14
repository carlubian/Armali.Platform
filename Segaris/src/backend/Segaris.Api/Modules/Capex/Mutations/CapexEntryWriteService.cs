using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Capex.Mutations;

/// <summary>
/// Write-side operations for Capex entries: create, full replacement update, and
/// physical deletion. Each mutation runs in a single <c>SaveChangesAsync</c>
/// transaction, recalculates totals server-side through the domain, and replaces
/// the ordered item collection atomically. Authorization mirrors the read side:
/// a public entry is mutable by any user (collaboration) while a private entry is
/// mutable only by its creator, and only the creator may change visibility.
/// </summary>
internal sealed class CapexEntryWriteService(
    SegarisDbContext database,
    CapexCatalogValidator catalogValidator,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int> CreateAsync(
        CreateCapexEntryRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (values, items) = Map(
            request.Title,
            request.MovementType,
            request.Status,
            request.DueDate,
            request.CategoryId,
            request.SupplierId,
            request.CostCenterId,
            request.CurrencyId,
            request.Notes,
            request.Visibility,
            request.Items);

        // Shape, item-count, and amount validation happen in the domain factory;
        // catalog reference existence is checked before the row is persisted.
        var entry = CapexEntry.Create(values, items, actorId, clock.UtcNow);
        await catalogValidator.ValidateAsync(values, cancellationToken);

        database.Add(entry);
        await database.SaveChangesAsync(cancellationToken);
        return entry.Id;
    }

    public async Task<bool> UpdateAsync(
        int entryId,
        UpdateCapexEntryRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = await database.Set<CapexEntry>()
            .Where(CapexEntryPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == entryId)
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is null)
        {
            return false;
        }

        var (values, items) = Map(
            request.Title,
            request.MovementType,
            request.Status,
            request.DueDate,
            request.CategoryId,
            request.SupplierId,
            request.CostCenterId,
            request.CurrencyId,
            request.Notes,
            request.Visibility,
            request.Items);

        // The domain applies shape validation and the creator-only visibility
        // policy and replaces the tracked item collection; catalog references are
        // validated before the single transactional save.
        entry.Update(values, items, actorId, clock.UtcNow);
        await catalogValidator.ValidateAsync(values, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int entryId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var entry = await database.Set<CapexEntry>()
            .Where(CapexEntryPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == entryId)
            .FirstOrDefaultAsync(cancellationToken);
        if (entry is null)
        {
            return false;
        }

        // Deletion is physical and cascades to items at the database level.
        database.Remove(entry);
        await database.SaveChangesAsync(cancellationToken);

        // Compensating storage cleanup runs after the entry row is gone. Files are
        // outside the database transaction, so any residue is reconciled later
        // rather than resurrecting the deleted entry.
        var owner = CapexAttachments.Owner(entryId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    private static (CapexEntryValues Values, IReadOnlyList<CapexItemValues> Items) Map(
        string? title,
        string? movementType,
        string? status,
        DateOnly dueDate,
        int categoryId,
        int? supplierId,
        int? costCenterId,
        int currencyId,
        string? notes,
        string? visibility,
        IReadOnlyList<CapexItemRequest> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var values = new CapexEntryValues(
            title ?? string.Empty,
            ParseEnum<CapexMovementType>(movementType, "movement type"),
            ParseEnum<CapexEntryStatus>(status, "status"),
            dueDate,
            categoryId,
            supplierId,
            costCenterId,
            currencyId,
            notes,
            ParseEnum<RecordVisibility>(visibility, "visibility"));

        var itemValues = items
            .Select(item => new CapexItemValues(item.Description ?? string.Empty, item.Quantity, item.UnitAmount))
            .ToArray();

        return (values, itemValues);
    }

    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new CapexValidationException($"The {field} is not a recognized value.");
        }

        return parsed;
    }
}
