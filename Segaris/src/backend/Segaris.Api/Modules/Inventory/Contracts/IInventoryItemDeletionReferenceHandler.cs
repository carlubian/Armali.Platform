using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Contracts;

/// <summary>
/// Deletion-reference contract published by Inventory and implemented by every
/// module that references an item (initially Recipes). When an item is deleted, the
/// Inventory deletion command resolves every registered handler, evaluates the
/// combined impact, and drives the link clearing inside the single
/// <c>SegarisDbContext</c> transaction it started. Inventory enumerates handlers; it
/// never queries the consuming module's entities.
///
/// Unlike the Assets reassignment contract, an item reference is optional, so item
/// deletion <b>clears</b> the link and never reassigns and never blocks: every
/// referencing line survives intact as free text.
///
/// Transaction ownership is fixed: a handler mutates tracked entities and updates
/// their audit metadata, but it must never call <c>SaveChanges</c> or commit. The
/// owner performs one final save and commit only after every handler succeeds; if
/// any handler or validation step fails, the whole transaction rolls back and no
/// reference, audit field, or item row changes.
///
/// Keeping the interface in the Inventory namespace preserves the
/// <c>Recipes -> Inventory</c> dependency direction: Recipes implements and registers
/// handlers, Inventory never references Recipes.
/// </summary>
internal interface IInventoryItemDeletionReferenceHandler
{
    /// <summary>
    /// Reports how many records reference the item without disclosing identities or
    /// private records of other users. The count includes public and private records
    /// so the owner can present a privacy-neutral deletion impact.
    /// </summary>
    Task<int> CountReferencesAsync(int itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the link on every record referencing the deleted item and updates the
    /// affected records' modification metadata. The reference is set to
    /// <see langword="null"/>; it is never reassigned and deletion is never blocked.
    /// Does not save or commit.
    /// </summary>
    Task ClearReferencesAsync(
        InventoryItemDeletionClearing clearing,
        CancellationToken cancellationToken);
}

/// <summary>
/// The clearing a handler must apply when a referenced item is deleted. Every
/// reference to <see cref="ItemId"/> is cleared to <see langword="null"/> regardless
/// of record status or ownership; the reference is never reassigned.
/// </summary>
/// <param name="ItemId">The item being deleted.</param>
/// <param name="Actor">The user performing the deletion, for audit.</param>
/// <param name="OccurredAt">The UTC modification time stamped on affected records.</param>
internal sealed record InventoryItemDeletionClearing(
    int ItemId,
    UserId Actor,
    DateTimeOffset OccurredAt);
