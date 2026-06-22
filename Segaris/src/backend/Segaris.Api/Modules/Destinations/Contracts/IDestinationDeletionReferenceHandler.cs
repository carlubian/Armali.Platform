using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Contracts;

/// <summary>
/// Deletion-reference contract published by Destinations and implemented by every
/// module that references a destination (initially Travel). When a destination is
/// deleted, Destinations resolves every registered handler, evaluates the combined
/// impact, and drives link clearing inside the single <c>SegarisDbContext</c>
/// transaction it started. Destinations enumerates handlers; it never queries the
/// consuming module's entities.
///
/// The reference is optional, so destination deletion clears the link and never
/// reassigns and never blocks.
///
/// Transaction ownership is fixed: a handler mutates tracked entities and updates
/// their audit metadata, but it must never call <c>SaveChanges</c> or commit. The
/// owner performs one final save and commit only after every handler succeeds; if
/// any handler or validation step fails, the whole transaction rolls back and no
/// reference, audit field, or destination row changes.
///
/// Keeping the interface in the Destinations namespace preserves the
/// <c>Travel -> Destinations</c> dependency direction: Travel implements and
/// registers handlers, Destinations never references Travel.
/// </summary>
internal interface IDestinationDeletionReferenceHandler
{
    /// <summary>
    /// Reports how many records reference the destination without disclosing
    /// identities or private records of other users. The count includes public and
    /// private records so Destinations can present a privacy-neutral deletion impact.
    /// </summary>
    Task<int> CountReferencesAsync(int destinationId, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the link on every record referencing the deleted destination and
    /// updates the affected records' modification metadata. The reference is set to
    /// <see langword="null"/>; it is never reassigned and deletion is never blocked.
    /// Does not save or commit.
    /// </summary>
    Task ClearReferencesAsync(
        DestinationDeletionClearing clearing,
        CancellationToken cancellationToken);
}

/// <summary>
/// The clearing a handler must apply when a referenced destination is deleted.
/// Every reference to <see cref="DestinationId"/> is cleared to
/// <see langword="null"/> regardless of record status or ownership.
/// </summary>
/// <param name="DestinationId">The destination being deleted.</param>
/// <param name="Actor">The user performing the deletion, for audit.</param>
/// <param name="OccurredAt">The UTC modification time stamped on affected records.</param>
internal sealed record DestinationDeletionClearing(
    int DestinationId,
    UserId Actor,
    DateTimeOffset OccurredAt);
