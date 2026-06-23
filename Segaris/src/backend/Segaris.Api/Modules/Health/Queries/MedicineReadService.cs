using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Queries;

/// <summary>
/// Read-side queries for Health medicines. Wave 3 deliberately leaves Inventory
/// resolution and attachments out of the projection.
/// </summary>
internal sealed class MedicineReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<MedicineSummaryResponse>> ListMedicinesAsync(
        MedicineFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var medicines = ApplyFilters(
            database.Set<Medicine>().AsNoTracking().Where(HealthMedicinePolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await medicines.CountAsync(cancellationToken);

        var page = await ApplySort(medicines, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(medicine => new MedicineSummaryResponse(
                medicine.Id,
                medicine.Name,
                medicine.CategoryId,
                database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First(),
                medicine.RequiresPrescription,
                null,
                null,
                medicine.Visibility.ToString(),
                new MedicineThumbnailResponse(null, null, "Placeholder"),
                medicine.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<MedicineSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<MedicineResponse?> GetMedicineAsync(
        int medicineId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        return await database.Set<Medicine>()
            .AsNoTracking()
            .Where(HealthMedicinePolicies.AccessibleTo(userId))
            .Where(medicine => medicine.Id == medicineId)
            .Select(medicine => new MedicineResponse(
                medicine.Id,
                medicine.Name,
                medicine.CategoryId,
                database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First(),
                medicine.Posology,
                medicine.RequiresPrescription,
                null,
                null,
                medicine.Notes,
                medicine.Visibility.ToString(),
                Array.Empty<MedicineAttachmentResponse>(),
                medicine.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.CreatedBy).Select(user => user.DisplayName).First(),
                medicine.CreatedAt,
                medicine.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.UpdatedBy).Select(user => user.DisplayName).First(),
                medicine.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Medicine> ApplyFilters(
        IQueryable<Medicine> medicines,
        MedicineFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            medicines = medicines.Where(medicine => EF.Functions.Like(medicine.Name.ToLower(), pattern, "\\"));
        }

        if (filter.CategoryId is { } categoryId)
        {
            medicines = medicines.Where(medicine => medicine.CategoryId == categoryId);
        }

        if (filter.RequiresPrescription is { } requiresPrescription)
        {
            medicines = medicines.Where(medicine => medicine.RequiresPrescription == requiresPrescription);
        }

        if (filter.Visibility is { } visibility)
        {
            medicines = medicines.Where(medicine => medicine.Visibility == visibility);
        }

        if (filter.CreatorId is { } creatorId)
        {
            medicines = medicines.Where(medicine => medicine.CreatedBy == creatorId);
        }

        return medicines;
    }

    private IQueryable<Medicine> ApplySort(IQueryable<Medicine> medicines, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<Medicine> ordered = sort.Field switch
        {
            MedicineQuery.SortFields.Name => ascending
                ? medicines.OrderBy(medicine => medicine.Name)
                : medicines.OrderByDescending(medicine => medicine.Name),
            MedicineQuery.SortFields.Category => ascending
                ? medicines.OrderBy(medicine => database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First())
                : medicines.OrderByDescending(medicine => database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First()),
            MedicineQuery.SortFields.TieBreaker => ascending
                ? medicines.OrderBy(medicine => medicine.Id)
                : medicines.OrderByDescending(medicine => medicine.Id),
            _ => ascending
                ? medicines.OrderBy(medicine => medicine.Name)
                : medicines.OrderByDescending(medicine => medicine.Name),
        };

        return ascending ? ordered.ThenBy(medicine => medicine.Id) : ordered.ThenByDescending(medicine => medicine.Id);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
