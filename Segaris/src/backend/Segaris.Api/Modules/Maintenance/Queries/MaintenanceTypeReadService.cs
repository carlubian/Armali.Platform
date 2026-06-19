using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Maintenance.Queries;

/// <summary>
/// Reads the Maintenance-owned <c>MaintenanceType</c> catalogue in its declared order
/// (<c>SortOrder</c>, then <c>Id</c>) for both the task editor and the Configuration
/// presentation.
/// </summary>
internal sealed class MaintenanceTypeReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<MaintenanceTypeResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await database.Set<MaintenanceType>()
            .AsNoTracking()
            .OrderBy(type => type.SortOrder)
            .ThenBy(type => type.Id)
            .Select(type => new MaintenanceTypeResponse(type.Id, type.Name, type.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
