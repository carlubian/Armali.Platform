using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Queries;

/// <summary>
/// Read-side queries for Health medicines. Wave 5 resolves the gallery thumbnail and
/// the medicine attachment list from the shared attachment subsystem; Inventory item
/// resolution remains deferred to Wave 6. Every medicine query filters to accessible
/// records before projection, pagination, or detail lookup.
/// </summary>
internal sealed class MedicineReadService(SegarisDbContext database, IAttachmentService attachments)
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

        var rows = await ApplySort(medicines, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(medicine => new MedicineSummaryRow(
                medicine.Id,
                medicine.Name,
                medicine.CategoryId,
                database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First(),
                medicine.RequiresPrescription,
                medicine.Visibility,
                medicine.PrimaryAttachmentId,
                medicine.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        var page = new MedicineSummaryResponse[rows.Length];
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var descriptors = await attachments.ListByOwnerAsync(
                HealthAttachments.MedicineOwner(row.Id),
                cancellationToken);
            page[index] = new MedicineSummaryResponse(
                row.Id,
                row.Name,
                row.CategoryId,
                row.CategoryName,
                row.RequiresPrescription,
                null,
                null,
                row.Visibility.ToString(),
                ResolveThumbnail(row.Id, row.PrimaryAttachmentId, descriptors),
                row.CreatorId,
                row.CreatorName);
        }

        return PaginatedResponse<MedicineSummaryResponse>.Create(page, pagination, totalCount);
    }

    public async Task<MedicineResponse?> GetMedicineAsync(
        int medicineId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Medicine>()
            .AsNoTracking()
            .Where(HealthMedicinePolicies.AccessibleTo(userId))
            .Where(medicine => medicine.Id == medicineId)
            .Select(medicine => new MedicineDetailRow(
                medicine.Id,
                medicine.Name,
                medicine.CategoryId,
                database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First(),
                medicine.Posology,
                medicine.RequiresPrescription,
                medicine.Notes,
                medicine.Visibility,
                medicine.PrimaryAttachmentId,
                medicine.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.CreatedBy).Select(user => user.DisplayName).First(),
                medicine.CreatedAt,
                medicine.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.UpdatedBy).Select(user => user.DisplayName).First(),
                medicine.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(
            HealthAttachments.MedicineOwner(row.Id),
            cancellationToken);
        return new MedicineResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.Posology,
            row.RequiresPrescription,
            null,
            null,
            row.Notes,
            row.Visibility.ToString(),
            descriptors.Select(descriptor => ToAttachmentResponse(descriptor, row.PrimaryAttachmentId)).ToArray(),
            row.CreatorId,
            row.CreatorName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    public Task<bool> MedicineAccessibleAsync(
        int medicineId,
        UserId userId,
        CancellationToken cancellationToken) =>
        database.Set<Medicine>()
            .AsNoTracking()
            .Where(HealthMedicinePolicies.AccessibleTo(userId))
            .AnyAsync(medicine => medicine.Id == medicineId, cancellationToken);

    /// <summary>
    /// Lists the attachments of an accessible medicine, flagging the primary image.
    /// Returns <c>null</c> when the medicine is missing or inaccessible so the caller can
    /// report not-found without disclosing private data.
    /// </summary>
    public async Task<IReadOnlyList<MedicineAttachmentResponse>?> ListMedicineAttachmentsAsync(
        int medicineId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var medicine = await database.Set<Medicine>()
            .AsNoTracking()
            .Where(HealthMedicinePolicies.AccessibleTo(userId))
            .Where(candidate => candidate.Id == medicineId)
            .Select(candidate => new { candidate.PrimaryAttachmentId })
            .FirstOrDefaultAsync(cancellationToken);
        if (medicine is null)
        {
            return null;
        }

        var descriptors = await attachments.ListByOwnerAsync(
            HealthAttachments.MedicineOwner(medicineId),
            cancellationToken);
        return descriptors
            .Select(descriptor => ToAttachmentResponse(descriptor, medicine.PrimaryAttachmentId))
            .ToArray();
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

    /// <summary>
    /// Resolves the gallery thumbnail: the primary image when it is set and still an
    /// image attachment, otherwise the first image attachment, otherwise a neutral
    /// placeholder. <paramref name="descriptors"/> arrive in upload order.
    /// </summary>
    private static MedicineThumbnailResponse ResolveThumbnail(
        int medicineId,
        int? primaryAttachmentId,
        IReadOnlyList<AttachmentDescriptor> descriptors)
    {
        var images = descriptors
            .Where(descriptor => HealthAttachments.IsImageContentType(descriptor.ContentType))
            .ToArray();

        if (primaryAttachmentId is { } primaryId
            && images.FirstOrDefault(descriptor => descriptor.Id.Value == primaryId) is { } primary)
        {
            return new(
                primary.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(medicineId, primary.Id.Value),
                "primary");
        }

        if (images.Length > 0)
        {
            var first = images[0];
            return new(
                first.Id.Value.ToString(CultureInfo.InvariantCulture),
                DownloadUrl(medicineId, first.Id.Value),
                "firstImage");
        }

        return new(null, null, "placeholder");
    }

    private static MedicineAttachmentResponse ToAttachmentResponse(
        AttachmentDescriptor descriptor,
        int? primaryAttachmentId) => new(
        descriptor.Id.Value.ToString(CultureInfo.InvariantCulture),
        descriptor.FileName,
        descriptor.ContentType,
        descriptor.Size,
        descriptor.CreatedBy.Value,
        descriptor.CreatedAt,
        descriptor.Id.Value == primaryAttachmentId);

    private static string DownloadUrl(int medicineId, int attachmentId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"/api/health/medicines/{medicineId}/attachments/{attachmentId}");

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record MedicineSummaryRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        bool RequiresPrescription,
        RecordVisibility Visibility,
        int? PrimaryAttachmentId,
        int CreatorId,
        string CreatorName);

    private sealed record MedicineDetailRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        string? Posology,
        bool RequiresPrescription,
        string? Notes,
        RecordVisibility Visibility,
        int? PrimaryAttachmentId,
        int CreatorId,
        string CreatorName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt);
}
