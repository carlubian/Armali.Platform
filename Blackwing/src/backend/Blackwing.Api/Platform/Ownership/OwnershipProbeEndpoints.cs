using Blackwing.Api.Modules.Identity.Security;
using Blackwing.Api.Platform.Api;
using Blackwing.Persistence;
using Blackwing.Persistence.Ownership;
using Blackwing.Shared.Time;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.Platform.Ownership;

/// <summary>
/// Endpoints over the ownership canary. They exist so the privacy perimeter can be demonstrated
/// end to end with two real accounts before any product entity exists.
/// </summary>
/// <remarks>
/// Phase 3 decides whether this resource is retired when <c>Image</c> arrives, or kept
/// permanently as a canary whose isolation tests fail loudly if the perimeter ever regresses.
/// </remarks>
internal static class OwnershipProbeEndpoints
{
    private const int MaximumLabelLength = 200;

    public static void MapOwnershipProbeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapBlackwingApiGroup("platform/ownership", "Ownership perimeter")
            .RequireAuthorization();

        group.MapGet("", ListAsync)
            .WithSummary("Lists the current user's ownership probes");

        group.MapPost("", CreateAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Creates an ownership probe owned by the current user");

        group.MapGet("/{id:int}", GetAsync)
            .WithSummary("Returns one of the current user's ownership probes");

        group.MapDelete("/{id:int}", DeleteAsync)
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithSummary("Deletes one of the current user's ownership probes");
    }

    private static async Task<IResult> ListAsync(
        BlackwingDbContext database,
        CancellationToken cancellationToken)
    {
        // No owner predicate is written here and none is needed: the global query filter has
        // already narrowed the set to the current account.
        var probes = await database.OwnershipProbes
            .AsNoTracking()
            .OrderBy(probe => probe.Id)
            .Select(probe => new OwnedProbeResponse(probe.Id, probe.Label, probe.CreatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(probes);
    }

    private static async Task<IResult> CreateAsync(
        CreateProbeRequest request,
        BlackwingDbContext database,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var label = request.Label?.Trim() ?? string.Empty;
        if (label.Length is 0 or > MaximumLabelLength)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["label"] = [$"Label must contain between 1 and {MaximumLabelLength} characters."],
                });
        }

        // The contract does not accept an owner, so impersonation is not merely rejected: it
        // cannot be expressed. The context stamps the owner on save.
        var probe = new OwnedProbe
        {
            Label = label,
            CreatedAt = clock.UtcNow,
        };

        database.OwnershipProbes.Add(probe);
        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/api/platform/ownership/{probe.Id}",
            new OwnedProbeResponse(probe.Id, probe.Label, probe.CreatedAt));
    }

    private static async Task<IResult> GetAsync(
        int id,
        BlackwingDbContext database,
        CancellationToken cancellationToken)
    {
        // Another account's probe is filtered out of the query and therefore answers 404, never
        // 403: a 403 would confirm that the identifier exists, which is itself a leak.
        var probe = await database.OwnershipProbes
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new OwnedProbeResponse(candidate.Id, candidate.Label, candidate.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return probe is null ? throw ApiProblemException.NotFound() : TypedResults.Ok(probe);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        BlackwingDbContext database,
        CancellationToken cancellationToken)
    {
        var probe = await database.OwnershipProbes
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw ApiProblemException.NotFound();

        database.OwnershipProbes.Remove(probe);
        await database.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    internal sealed record CreateProbeRequest(string? Label);

    internal sealed record OwnedProbeResponse(int Id, string Label, DateTimeOffset CreatedAt);
}
