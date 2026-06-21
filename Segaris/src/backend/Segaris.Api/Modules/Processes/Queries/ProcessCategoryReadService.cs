using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Processes.Queries;

/// <summary>
/// Reads the Processes-owned <c>ProcessCategory</c> catalogue in its declared order
/// (<c>SortOrder</c>, then <c>Id</c>) for both the process editor and the Configuration
/// presentation.
/// </summary>
internal sealed class ProcessCategoryReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<ProcessCategoryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await database.Set<ProcessCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new ProcessCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
