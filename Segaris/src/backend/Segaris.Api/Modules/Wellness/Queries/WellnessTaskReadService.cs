using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Wellness.Queries;

/// <summary>
/// Reads the administrator-managed, household-shared task catalogue in creation
/// order. Order is the creation-order <c>SortOrder</c> and is not user-editable.
/// </summary>
internal sealed class WellnessTaskReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<WellnessTaskResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await database.Set<WellnessTask>()
            .AsNoTracking()
            .OrderBy(task => task.SortOrder)
            .ThenBy(task => task.Id)
            .Select(task => new WellnessTaskResponse(task.Id, task.Name, task.Category.ToString(), task.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
