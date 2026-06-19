using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Maintenance.Queries;

/// <summary>
/// Read-side queries for Maintenance tasks. Asset names are intentionally unresolved
/// in Wave 2; Wave 3 supplies them through the Assets read contract.
/// </summary>
internal sealed class MaintenanceTaskReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<MaintenanceTaskSummaryResponse>> ListTasksAsync(
        MaintenanceTaskFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var tasks = ApplyFilters(
            database.Set<MaintenanceTask>().AsNoTracking().Where(MaintenanceTaskPolicies.AccessibleTo(userId)),
            filter);

        var totalCount = await tasks.CountAsync(cancellationToken);

        var items = await ApplySort(tasks, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(task => new MaintenanceTaskSummaryResponse(
                task.Id,
                task.Title,
                task.MaintenanceTypeId,
                database.Set<MaintenanceType>()
                    .Where(type => type.Id == task.MaintenanceTypeId).Select(type => type.Name).First(),
                task.Status.ToString(),
                task.Priority.ToString(),
                task.AssetId,
                null,
                task.DueDate,
                task.Visibility.ToString(),
                task.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == task.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<MaintenanceTaskSummaryResponse>.Create(items, pagination, totalCount);
    }

    public async Task<MaintenanceTaskResponse?> GetTaskAsync(
        int taskId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        return await database.Set<MaintenanceTask>()
            .AsNoTracking()
            .Where(MaintenanceTaskPolicies.AccessibleTo(userId))
            .Where(task => task.Id == taskId)
            .Select(task => new MaintenanceTaskResponse(
                task.Id,
                task.Title,
                task.MaintenanceTypeId,
                database.Set<MaintenanceType>()
                    .Where(type => type.Id == task.MaintenanceTypeId).Select(type => type.Name).First(),
                task.Status.ToString(),
                task.Priority.ToString(),
                task.AssetId,
                null,
                task.DueDate,
                task.CompletedDate,
                task.Notes,
                task.Visibility.ToString(),
                Array.Empty<MaintenanceTaskAttachmentResponse>(),
                task.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == task.CreatedBy).Select(user => user.DisplayName).First(),
                task.CreatedAt,
                task.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == task.UpdatedBy).Select(user => user.DisplayName).First(),
                task.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<MaintenanceTask> ApplyFilters(
        IQueryable<MaintenanceTask> tasks,
        MaintenanceTaskFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            tasks = tasks.Where(task =>
                EF.Functions.Like(task.Title.ToLower(), pattern, "\\")
                || (task.Notes != null && EF.Functions.Like(task.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.TypeId is { } typeId)
        {
            tasks = tasks.Where(task => task.MaintenanceTypeId == typeId);
        }

        if (filter.Status is { } status)
        {
            tasks = tasks.Where(task => task.Status == status);
        }

        if (filter.Priority is { } priority)
        {
            tasks = tasks.Where(task => task.Priority == priority);
        }

        if (filter.CreatorId is { } creatorId)
        {
            tasks = tasks.Where(task => task.CreatedBy == creatorId);
        }

        if (filter.Visibility is { } visibility)
        {
            tasks = tasks.Where(task => task.Visibility == visibility);
        }

        return tasks;
    }

    private IQueryable<MaintenanceTask> ApplySort(IQueryable<MaintenanceTask> tasks, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<MaintenanceTask> ordered = sort.Field switch
        {
            MaintenanceQuery.SortFields.Title => ascending
                ? tasks.OrderBy(task => task.Title)
                : tasks.OrderByDescending(task => task.Title),
            MaintenanceQuery.SortFields.Type => ascending
                ? tasks.OrderBy(task => database.Set<MaintenanceType>()
                    .Where(type => type.Id == task.MaintenanceTypeId).Select(type => type.Name).First())
                : tasks.OrderByDescending(task => database.Set<MaintenanceType>()
                    .Where(type => type.Id == task.MaintenanceTypeId).Select(type => type.Name).First()),
            MaintenanceQuery.SortFields.Status => ascending
                ? tasks.OrderBy(task => task.Status)
                : tasks.OrderByDescending(task => task.Status),
            MaintenanceQuery.SortFields.Priority => ascending
                ? tasks.OrderBy(task => task.Priority)
                : tasks.OrderByDescending(task => task.Priority),
            MaintenanceQuery.SortFields.Visibility => ascending
                ? tasks.OrderBy(task => task.Visibility)
                : tasks.OrderByDescending(task => task.Visibility),
            MaintenanceQuery.SortFields.Id => ascending
                ? tasks.OrderBy(task => task.Id)
                : tasks.OrderByDescending(task => task.Id),
            _ => ApplyDueDateSort(tasks, ascending),
        };

        return ascending ? ordered.ThenBy(task => task.Id) : ordered.ThenByDescending(task => task.Id);
    }

    private static IOrderedQueryable<MaintenanceTask> ApplyDueDateSort(
        IQueryable<MaintenanceTask> tasks,
        bool ascending) =>
        ascending
            ? tasks.OrderBy(task => task.DueDate == null).ThenBy(task => task.DueDate)
            : tasks.OrderBy(task => task.DueDate == null).ThenByDescending(task => task.DueDate);

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
