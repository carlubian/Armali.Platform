using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Queries;

/// <summary>
/// Read-side queries for Health diseases. Every query filters to accessible records
/// before projection, pagination, or detail lookup.
/// </summary>
internal sealed class DiseaseReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<DiseaseSummaryResponse>> ListDiseasesAsync(
        DiseaseFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var diseases = ApplyFilters(
            database.Set<Disease>().AsNoTracking().Where(HealthDiseasePolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await diseases.CountAsync(cancellationToken);

        var page = await ApplySort(diseases, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(disease => new DiseaseSummaryResponse(
                disease.Id,
                disease.Name,
                disease.CategoryId,
                database.Set<DiseaseCategory>()
                    .Where(category => category.Id == disease.CategoryId).Select(category => category.Name).First(),
                disease.Visibility.ToString(),
                database.Set<DiseaseMedicine>()
                    .Where(association => association.DiseaseId == disease.Id)
                    .Join(
                        database.Set<Medicine>().Where(HealthMedicinePolicies.AccessibleTo(userId)),
                        association => association.MedicineId,
                        medicine => medicine.Id,
                        (association, _) => association)
                    .Count(),
                disease.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == disease.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<DiseaseSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<DiseaseResponse?> GetDiseaseAsync(
        int diseaseId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        return await database.Set<Disease>()
            .AsNoTracking()
            .Where(HealthDiseasePolicies.AccessibleTo(userId))
            .Where(disease => disease.Id == diseaseId)
            .Select(disease => new DiseaseResponse(
                disease.Id,
                disease.Name,
                disease.CategoryId,
                database.Set<DiseaseCategory>()
                    .Where(category => category.Id == disease.CategoryId).Select(category => category.Name).First(),
                disease.Symptoms,
                disease.AverageDurationDays,
                disease.Notes,
                disease.Visibility.ToString(),
                disease.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == disease.CreatedBy).Select(user => user.DisplayName).First(),
                disease.CreatedAt,
                disease.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == disease.UpdatedBy).Select(user => user.DisplayName).First(),
                disease.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Disease> ApplyFilters(
        IQueryable<Disease> diseases,
        DiseaseFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            diseases = diseases.Where(disease => EF.Functions.Like(disease.Name.ToLower(), pattern, "\\"));
        }

        if (filter.CategoryId is { } categoryId)
        {
            diseases = diseases.Where(disease => disease.CategoryId == categoryId);
        }

        if (filter.Visibility is { } visibility)
        {
            diseases = diseases.Where(disease => disease.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            diseases = diseases.Where(disease => disease.CreatedBy == creatorId);
        }

        return diseases;
    }

    private IQueryable<Disease> ApplySort(IQueryable<Disease> diseases, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Disease> ordered = sort.Field switch
        {
            DiseaseQuery.SortFields.Name => ascending
                ? diseases.OrderBy(disease => disease.Name)
                : diseases.OrderByDescending(disease => disease.Name),
            DiseaseQuery.SortFields.Category => ascending
                ? diseases.OrderBy(disease => database.Set<DiseaseCategory>()
                    .Where(category => category.Id == disease.CategoryId).Select(category => category.Name).First())
                : diseases.OrderByDescending(disease => database.Set<DiseaseCategory>()
                    .Where(category => category.Id == disease.CategoryId).Select(category => category.Name).First()),
            DiseaseQuery.SortFields.TieBreaker => ascending
                ? diseases.OrderBy(disease => disease.Id)
                : diseases.OrderByDescending(disease => disease.Id),
            _ => ascending
                ? diseases.OrderBy(disease => disease.Name)
                : diseases.OrderByDescending(disease => disease.Name),
        };

        return ascending ? ordered.ThenBy(disease => disease.Id) : ordered.ThenByDescending(disease => disease.Id);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
