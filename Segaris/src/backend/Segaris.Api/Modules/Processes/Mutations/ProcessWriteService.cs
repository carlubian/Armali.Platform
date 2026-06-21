using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Processes.Mutations;

/// <summary>
/// Write-side operations on Processes. Inaccessible processes are reported as not found so
/// private records are never disclosed.
/// </summary>
internal sealed class ProcessWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateProcessRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = await MapCreateAsync(request, cancellationToken);
        var process = Process.Create(values, actorId, clock.UtcNow);
        database.Add(process);
        await database.SaveChangesAsync(cancellationToken);
        return process.Id;
    }

    public async Task<bool> UpdateAsync(
        int processId,
        UpdateProcessRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await database.Set<Process>()
            .Where(ProcessPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == processId)
            .FirstOrDefaultAsync(cancellationToken);
        if (process is null)
        {
            return false;
        }

        var values = await MapUpdateAsync(request, cancellationToken);
        if (values.Visibility != process.Visibility && !ProcessPolicies.CanChangeVisibility(process, actorId))
        {
            throw new ProcessesValidationException(
                "Only the creator may change process visibility.",
                ProcessesValidationReason.VisibilityForbidden);
        }

        process.Update(values, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int processId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var process = await database.Set<Process>()
            .Where(ProcessPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == processId)
            .FirstOrDefaultAsync(cancellationToken);
        if (process is null)
        {
            return false;
        }

        database.Remove(process);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(
        int processId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var process = await FindMutableAsync(processId, actorId, cancellationToken);
        if (process is null)
        {
            return false;
        }

        process.Cancel(actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReopenAsync(
        int processId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var process = await FindMutableAsync(processId, actorId, cancellationToken);
        if (process is null)
        {
            return false;
        }

        process.Reopen(actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateStepsAsync(
        int processId,
        UpdateStepListRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await FindMutableAsync(processId, actorId, cancellationToken);
        if (process is null)
        {
            return false;
        }

        var requestedSteps = request.Steps
            ?? throw new ProcessesValidationException(
                "The step list is required.",
                ProcessesValidationReason.StepValidation);
        var existingSteps = await LoadOrderedStepsAsync(processId, cancellationToken);
        var existingById = existingSteps.ToDictionary(step => step.Id);
        var requestedIds = new HashSet<int>();
        var finalSnapshots = new List<StepSnapshot>(requestedSteps.Count);

        for (var sortOrder = 0; sortOrder < requestedSteps.Count; sortOrder++)
        {
            var requested = requestedSteps[sortOrder];
            if (requested.Id is { } stepId)
            {
                if (stepId <= 0 || !requestedIds.Add(stepId))
                {
                    throw new ProcessesValidationException(
                        "Step identifiers must be unique positive values.",
                        ProcessesValidationReason.StepValidation);
                }

                if (!existingById.TryGetValue(stepId, out var existing))
                {
                    throw new ProcessesValidationException(
                        "Step not found.",
                        ProcessesValidationReason.StepNotFound);
                }

                finalSnapshots.Add(new StepSnapshot(existing.State, requested.IsOptional));
            }
            else
            {
                finalSnapshots.Add(new StepSnapshot(StepExecutionState.Pending, requested.IsOptional));
            }
        }

        EnsureContiguous(finalSnapshots);

        for (var sortOrder = 0; sortOrder < requestedSteps.Count; sortOrder++)
        {
            var requested = requestedSteps[sortOrder];
            var values = new StepValues(requested.Description, requested.DueDate, requested.Notes, requested.IsOptional);

            if (requested.Id is { } stepId)
            {
                existingById[stepId].Update(values, sortOrder, actorId, clock.UtcNow);
            }
            else
            {
                database.Add(Step.Create(processId, values, sortOrder, actorId, clock.UtcNow));
            }
        }

        foreach (var existing in existingSteps)
        {
            if (!requestedIds.Contains(existing.Id))
            {
                database.Remove(existing);
            }
        }

        process.MarkStepsChanged(actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> CompleteStepAsync(int processId, int stepId, UserId actorId, CancellationToken cancellationToken) =>
        ApplyFrontierActionAsync(processId, stepId, actorId, StepFrontierAction.Complete, cancellationToken);

    public Task<bool> SkipStepAsync(int processId, int stepId, UserId actorId, CancellationToken cancellationToken) =>
        ApplyFrontierActionAsync(processId, stepId, actorId, StepFrontierAction.Skip, cancellationToken);

    public Task<bool> UndoStepAsync(int processId, int stepId, UserId actorId, CancellationToken cancellationToken) =>
        ApplyFrontierActionAsync(processId, stepId, actorId, StepFrontierAction.Undo, cancellationToken);

    private async Task<Process?> FindMutableAsync(
        int processId,
        UserId actorId,
        CancellationToken cancellationToken) =>
        await database.Set<Process>()
            .Where(ProcessPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == processId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<List<Step>> LoadOrderedStepsAsync(int processId, CancellationToken cancellationToken) =>
        await database.Set<Step>()
            .Where(step => step.ProcessId == processId)
            .OrderBy(step => step.SortOrder)
            .ThenBy(step => step.Id)
            .ToListAsync(cancellationToken);

    private async Task<bool> ApplyFrontierActionAsync(
        int processId,
        int stepId,
        UserId actorId,
        StepFrontierAction action,
        CancellationToken cancellationToken)
    {
        var process = await FindMutableAsync(processId, actorId, cancellationToken);
        if (process is null)
        {
            return false;
        }

        var steps = await LoadOrderedStepsAsync(processId, cancellationToken);
        var target = steps.FirstOrDefault(step => step.Id == stepId)
            ?? throw new ProcessesValidationException(
                "Step not found.",
                ProcessesValidationReason.StepNotFound);
        var snapshots = steps.Select(step => step.Snapshot).ToArray();
        EnsureContiguous(snapshots);

        var frontierIndex = ProcessExecution.FrontierIndex(snapshots);
        var expected = action == StepFrontierAction.Undo
            ? MostRecentlyResolvedStep(steps, frontierIndex)
            : frontierIndex is { } index
                ? steps[index]
                : null;
        if (expected is null || expected.Id != target.Id)
        {
            throw new ProcessesValidationException(
                "The requested step is not at the executable frontier.",
                ProcessesValidationReason.FrontierViolation);
        }

        switch (action)
        {
            case StepFrontierAction.Complete:
                target.Complete(actorId, clock.UtcNow);
                break;
            case StepFrontierAction.Skip:
                target.Skip(actorId, clock.UtcNow);
                break;
            case StepFrontierAction.Undo:
                target.Reopen(actorId, clock.UtcNow);
                break;
            default:
                throw new InvalidOperationException("Unsupported step frontier action.");
        }

        process.MarkStepsChanged(actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Step? MostRecentlyResolvedStep(IReadOnlyList<Step> steps, int? frontierIndex)
    {
        if (steps.Count == 0)
        {
            return null;
        }

        var candidateIndex = frontierIndex is null ? steps.Count - 1 : frontierIndex.Value - 1;
        return candidateIndex >= 0 ? steps[candidateIndex] : null;
    }

    private static void EnsureContiguous(IReadOnlyList<StepSnapshot> snapshots)
    {
        if (!ProcessExecution.ResolvedFormContiguousPrefix(snapshots))
        {
            throw new ProcessesValidationException(
                "Resolved steps must remain a contiguous prefix.",
                ProcessesValidationReason.ContiguityViolation);
        }
    }

    private async Task<ProcessValues> MapCreateAsync(
        CreateProcessRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId < 0)
        {
            throw new ProcessesValidationException(
                "The process category does not exist.",
                ProcessesValidationReason.UnknownCategory);
        }

        var categoryId = request.CategoryId == 0
            ? await DefaultCategoryIdAsync(cancellationToken)
            : request.CategoryId;
        await ValidateCategoryAsync(categoryId, cancellationToken);

        return new ProcessValues(
            request.Name,
            categoryId,
            request.DueDate,
            request.Notes,
            ParseVisibility(request.Visibility, ProcessesDefaults.Visibility));
    }

    private async Task<ProcessValues> MapUpdateAsync(
        UpdateProcessRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId <= 0)
        {
            throw new ProcessesValidationException(
                "The process category does not exist.",
                ProcessesValidationReason.UnknownCategory);
        }

        await ValidateCategoryAsync(request.CategoryId, cancellationToken);

        return new ProcessValues(
            request.Name,
            request.CategoryId,
            request.DueDate,
            request.Notes,
            ParseVisibility(request.Visibility, ProcessesDefaults.Visibility));
    }

    private async Task<int> DefaultCategoryIdAsync(CancellationToken cancellationToken)
    {
        var categoryId = await database.Set<ProcessCategory>()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .Select(category => (int?)category.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return categoryId
            ?? throw new ProcessesValidationException(
                "The process category does not exist.",
                ProcessesValidationReason.UnknownCategory);
    }

    private async Task ValidateCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (categoryId <= 0
            || !await database.Set<ProcessCategory>().AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            throw new ProcessesValidationException(
                "The process category does not exist.",
                ProcessesValidationReason.UnknownCategory);
        }
    }

    private static RecordVisibility ParseVisibility(string? value, RecordVisibility defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<RecordVisibility>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ProcessesValidationException("The visibility is not a recognized value.");
    }

    private enum StepFrontierAction
    {
        Complete,
        Skip,
        Undo,
    }
}
