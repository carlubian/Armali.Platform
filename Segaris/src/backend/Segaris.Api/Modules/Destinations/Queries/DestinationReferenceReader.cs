using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Destinations.Queries;

/// <summary>Destinations-owned implementation of the cross-module destination reference contract.</summary>
internal sealed class DestinationReferenceReader(SegarisDbContext database) : IDestinationReferenceReader
{
    public Task<DestinationReference?> FindAccessibleAsync(
        int destinationId,
        UserId viewer,
        CancellationToken cancellationToken) =>
        database.Set<Destination>()
            .AsNoTracking()
            .Where(DestinationPolicies.AccessibleTo(viewer))
            .Where(destination => destination.Id == destinationId)
            .Select(destination => new DestinationReference(
                destination.Id,
                destination.Name,
                destination.Country,
                destination.Visibility))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, DestinationReference>> ResolveAccessibleAsync(
        IReadOnlyCollection<int> destinationIds,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        if (destinationIds.Count == 0)
        {
            return new Dictionary<int, DestinationReference>();
        }

        var ids = destinationIds.Distinct().ToArray();
        var rows = await database.Set<Destination>()
            .AsNoTracking()
            .Where(DestinationPolicies.AccessibleTo(viewer))
            .Where(destination => ids.Contains(destination.Id))
            .Select(destination => new DestinationReference(
                destination.Id,
                destination.Name,
                destination.Country,
                destination.Visibility))
            .ToArrayAsync(cancellationToken);

        return rows.ToDictionary(destination => destination.DestinationId);
    }
}
