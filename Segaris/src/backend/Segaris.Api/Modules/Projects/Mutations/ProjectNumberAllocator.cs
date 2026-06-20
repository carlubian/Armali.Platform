using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Projects.Mutations;

internal sealed class ProjectNumberAllocator(SegarisDbContext database, IClock clock)
{
    public async Task<int> AllocateAsync(CancellationToken cancellationToken)
    {
        var allocation = new ProjectNumberAllocation
        {
            AllocatedAt = clock.UtcNow,
        };

        database.Add(allocation);
        await database.SaveChangesAsync(cancellationToken);
        return allocation.Id;
    }
}
