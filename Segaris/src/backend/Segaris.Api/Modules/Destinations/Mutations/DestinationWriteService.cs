using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Destinations.Mutations;

/// <summary>
/// Write-side operations for destinations. Public destinations are collaboratively
/// mutable; private destinations remain creator-only and only creators can change
/// visibility.
/// </summary>
internal sealed class DestinationWriteService(SegarisDbContext database, IClock clock)
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
        return true;
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
