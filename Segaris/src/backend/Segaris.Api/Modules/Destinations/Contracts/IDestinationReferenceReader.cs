using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Contracts;

/// <summary>
/// The narrow, privacy-respecting projection of a destination published for
/// cross-module references. It carries only the stable identifier, display fields,
/// and visibility needed by Travel to enforce its trip visibility rule.
/// </summary>
internal sealed record DestinationReference(
    int DestinationId,
    string Name,
    string? Country,
    RecordVisibility Visibility);

/// <summary>
/// Narrow read contract published by Destinations and consumed by modules that hold
/// a live reference to a destination (initially Travel). It validates that a
/// referenced destination exists and is accessible to a viewer, resolves its
/// display name and country, and exposes its visibility so the consumer can apply
/// its own visibility rule.
///
/// Keeping this contract in the Destinations namespace preserves the
/// <c>Travel -> Destinations</c> dependency direction: Travel consumes the
/// contract, Destinations never references Travel.
/// </summary>
internal interface IDestinationReferenceReader
{
    /// <summary>
    /// Resolves a single destination reference for <paramref name="viewer"/>,
    /// applying Destinations accessibility rules. Returns <see langword="null"/>
    /// when the destination does not exist or is inaccessible, matching the platform
    /// not-found behaviour so private destinations are not disclosed.
    /// </summary>
    Task<DestinationReference?> FindAccessibleAsync(
        int destinationId,
        UserId viewer,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves display fields for several destinations in one query. Destinations
    /// that do not exist or are inaccessible to <paramref name="viewer"/> are
    /// omitted, so Travel renders a neutral placeholder for missing identifiers.
    /// </summary>
    Task<IReadOnlyDictionary<int, DestinationReference>> ResolveAccessibleAsync(
        IReadOnlyCollection<int> destinationIds,
        UserId viewer,
        CancellationToken cancellationToken);
}
