using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Api;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes.Queries;

/// <summary>Read-side queries for accessible Processes.</summary>
internal sealed class ProcessReadService(SegarisDbContext database)
{
    public async Task<PaginatedResponse<ProcessSummaryResponse>> ListAsync(
        ProcessFilter filter,
        PaginationRequest pagination,
        SortRequest sort,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var processes = ApplyFilters(
            database.Set<Process>().AsNoTracking().Where(ProcessPolicies.AccessibleTo(userId)),
            filter);

        var rowsQuery = processes.Select(process => new ProcessListRow
        {
            Id = process.Id,
            Name = process.Name,
            CategoryId = process.CategoryId,
            CategoryName = database.Set<ProcessCategory>()
                .Where(category => category.Id == process.CategoryId)
                .Select(category => category.Name)
                .First(),
            Status = process.IsCancelled
                ? ProcessExecution.CancelledStatusName
                : !database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State != StepExecutionState.Pending)
                    ? nameof(ProcessDerivedStatus.NotStarted)
                    : !database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State == StepExecutionState.Pending)
                        ? nameof(ProcessDerivedStatus.Completed)
                        : nameof(ProcessDerivedStatus.InProgress),
            IsCancelled = process.IsCancelled,
            ResolvedStepCount = database.Set<Step>().Count(step => step.ProcessId == process.Id && step.State != StepExecutionState.Pending),
            TotalStepCount = database.Set<Step>().Count(step => step.ProcessId == process.Id),
            EffectiveDueDate = process.DueDate
                ?? database.Set<Step>()
                    .Where(step => step.ProcessId == process.Id && step.State == StepExecutionState.Pending)
                    .OrderBy(step => step.SortOrder)
                    .ThenBy(step => step.Id)
                    .Select(step => step.DueDate)
                    .FirstOrDefault(),
            Visibility = process.Visibility.ToString(),
            CreatorId = process.CreatedBy,
            CreatorName = database.Set<SegarisUser>()
                .Where(user => user.Id == process.CreatedBy)
                .Select(user => user.DisplayName)
                .First(),
        });

        var totalCount = await rowsQuery.CountAsync(cancellationToken);
        var rows = await ApplySort(rowsQuery, sort)
            .Skip(pagination.Offset)
            .Take(pagination.PageSize)
            .Select(row => new ProcessSummaryResponse(
                row.Id,
                row.Name,
                row.CategoryId,
                row.CategoryName,
                row.Status,
                row.IsCancelled,
                row.ResolvedStepCount,
                row.TotalStepCount,
                row.EffectiveDueDate,
                row.Visibility,
                row.CreatorId,
                row.CreatorName))
            .ToArrayAsync(cancellationToken);

        return PaginatedResponse<ProcessSummaryResponse>.Create(rows, pagination, totalCount);
    }

    public async Task<ProcessResponse?> GetAsync(
        int processId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Process>()
            .AsNoTracking()
            .Where(ProcessPolicies.AccessibleTo(userId))
            .Where(process => process.Id == processId)
            .Select(process => new ProcessDetailRow(
                process.Id,
                process.Name,
                process.CategoryId,
                database.Set<ProcessCategory>()
                    .Where(category => category.Id == process.CategoryId)
                    .Select(category => category.Name)
                    .First(),
                process.IsCancelled,
                process.DueDate,
                process.Notes,
                process.Visibility.ToString(),
                process.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == process.CreatedBy)
                    .Select(user => user.DisplayName)
                    .First(),
                process.CreatedAt,
                process.UpdatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == process.UpdatedBy)
                    .Select(user => user.DisplayName)
                    .First(),
                process.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var steps = await database.Set<Step>()
            .AsNoTracking()
            .Where(step => step.ProcessId == processId)
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Id)
            .Select(step => new StepDetailRow(
                step.Id,
                step.Description,
                step.DueDate,
                step.Notes,
                step.IsOptional,
                step.State,
                step.SortOrder))
            .ToArrayAsync(cancellationToken);

        var snapshots = steps.Select(step => new StepSnapshot(step.State, step.IsOptional)).ToArray();
        var frontierIndex = ProcessExecution.FrontierIndex(snapshots);
        var effectiveDueDate = row.DueDate ?? (frontierIndex is { } index ? steps[index].DueDate : null);

        return new ProcessResponse(
            row.Id,
            row.Name,
            row.CategoryId,
            row.CategoryName,
            row.IsCancelled ? ProcessExecution.CancelledStatusName : ProcessExecution.DeriveStatus(snapshots).ToString(),
            row.IsCancelled,
            row.DueDate,
            effectiveDueDate,
            row.Notes,
            ProcessExecution.ResolvedCount(snapshots),
            steps.Length,
            frontierIndex is { } pendingIndex ? steps[pendingIndex].Id : null,
            row.Visibility,
            steps.Select(ToStepResponse).ToArray(),
            [],
            row.CreatedById,
            row.CreatedByName,
            row.CreatedAt,
            row.UpdatedById,
            row.UpdatedByName,
            row.UpdatedAt);
    }

    public async Task<bool> AccessibleAsync(int processId, UserId userId, CancellationToken cancellationToken) =>
        await database.Set<Process>()
            .AsNoTracking()
            .Where(ProcessPolicies.AccessibleTo(userId))
            .AnyAsync(process => process.Id == processId, cancellationToken);

    private IQueryable<Process> ApplyFilters(IQueryable<Process> processes, ProcessFilter filter)
    {
        if (filter.Search is { } search)
        {
            var pattern = $"%{Escape(search.ToLowerInvariant())}%";
            processes = processes.Where(process =>
                EF.Functions.Like(process.Name.ToLower(), pattern, "\\")
                || (process.Notes != null && EF.Functions.Like(process.Notes.ToLower(), pattern, "\\")));
        }

        if (filter.CategoryId is { } categoryId)
        {
            processes = processes.Where(process => process.CategoryId == categoryId);
        }

        if (filter.Status is { } status)
        {
            processes = ApplyStatusFilter(processes, status);
        }

        if (filter.CreatorId is { } creatorId)
        {
            processes = processes.Where(process => process.CreatedBy == creatorId);
        }

        if (filter.Visibility is { } visibility)
        {
            processes = processes.Where(process => process.Visibility == visibility);
        }

        return processes;
    }

    private IQueryable<Process> ApplyStatusFilter(IQueryable<Process> processes, string status) =>
        status switch
        {
            ProcessExecution.CancelledStatusName => processes.Where(process => process.IsCancelled),
            _ => ApplyDerivedStatusFilter(processes, status),
        };

    private IQueryable<Process> ApplyDerivedStatusFilter(IQueryable<Process> processes, string status)
    {
        if (status == nameof(ProcessDerivedStatus.NotStarted))
        {
            return processes.Where(process =>
                !process.IsCancelled
                && !database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State != StepExecutionState.Pending));
        }

        if (status == nameof(ProcessDerivedStatus.Completed))
        {
            return processes.Where(process =>
                !process.IsCancelled
                && database.Set<Step>().Any(step => step.ProcessId == process.Id)
                && !database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State == StepExecutionState.Pending));
        }

        return processes.Where(process =>
            !process.IsCancelled
            && database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State != StepExecutionState.Pending)
            && database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State == StepExecutionState.Pending));
    }

    private IOrderedQueryable<ProcessListRow> ApplySort(IQueryable<ProcessListRow> rows, SortRequest sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        IOrderedQueryable<ProcessListRow> ordered = sort.Field switch
        {
            ProcessesQuery.SortFields.Name => ascending
                ? rows.OrderBy(row => row.Name)
                : rows.OrderByDescending(row => row.Name),
            ProcessesQuery.SortFields.Category => ascending
                ? rows.OrderBy(row => row.CategoryName)
                : rows.OrderByDescending(row => row.CategoryName),
            ProcessesQuery.SortFields.Status => ascending
                ? rows.OrderBy(row => row.Status)
                : rows.OrderByDescending(row => row.Status),
            ProcessesQuery.SortFields.Visibility => ascending
                ? rows.OrderBy(row => row.Visibility)
                : rows.OrderByDescending(row => row.Visibility),
            ProcessesQuery.SortFields.Id => ascending
                ? rows.OrderBy(row => row.Id)
                : rows.OrderByDescending(row => row.Id),
            _ => ApplyDueDateSort(rows, ascending),
        };

        return ascending ? ordered.ThenBy(row => row.Id) : ordered.ThenByDescending(row => row.Id);
    }

    private static IOrderedQueryable<ProcessListRow> ApplyDueDateSort(
        IQueryable<ProcessListRow> rows,
        bool ascending) =>
        ascending
            ? rows.OrderBy(row => row.EffectiveDueDate == null).ThenBy(row => row.EffectiveDueDate)
            : rows.OrderBy(row => row.EffectiveDueDate == null).ThenByDescending(row => row.EffectiveDueDate);

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static StepResponse ToStepResponse(StepDetailRow step) => new(
        step.Id,
        step.Description,
        step.DueDate,
        step.Notes,
        step.IsOptional,
        step.State.ToString(),
        step.SortOrder);

    private sealed class ProcessListRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public bool IsCancelled { get; init; }
        public int ResolvedStepCount { get; init; }
        public int TotalStepCount { get; init; }
        public DateOnly? EffectiveDueDate { get; init; }
        public string Visibility { get; init; } = string.Empty;
        public int CreatorId { get; init; }
        public string CreatorName { get; init; } = string.Empty;
    }

    private sealed record ProcessDetailRow(
        int Id,
        string Name,
        int CategoryId,
        string CategoryName,
        bool IsCancelled,
        DateOnly? DueDate,
        string? Notes,
        string Visibility,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int? UpdatedById,
        string? UpdatedByName,
        DateTimeOffset? UpdatedAt);

    private sealed record StepDetailRow(
        int Id,
        string Description,
        DateOnly? DueDate,
        string? Notes,
        bool IsOptional,
        StepExecutionState State,
        int SortOrder);
}
