using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Health.Queries;

/// <summary>
/// Read-side queries for the Health-owned category catalogues. Both catalogues are
/// returned in deterministic catalogue order (sort order, then identifier).
/// </summary>
internal sealed class HealthCatalogReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<DiseaseCategoryResponse>> ListDiseaseCategoriesAsync(CancellationToken cancellationToken) =>
        await database.Set<DiseaseCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new DiseaseCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicineCategoryResponse>> ListMedicineCategoriesAsync(CancellationToken cancellationToken) =>
        await database.Set<MedicineCategory>()
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => new MedicineCategoryResponse(category.Id, category.Name, category.SortOrder))
            .ToArrayAsync(cancellationToken);
}
