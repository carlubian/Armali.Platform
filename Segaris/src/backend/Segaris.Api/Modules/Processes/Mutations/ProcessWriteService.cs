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

    private async Task<Process?> FindMutableAsync(
        int processId,
        UserId actorId,
        CancellationToken cancellationToken) =>
        await database.Set<Process>()
            .Where(ProcessPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == processId)
            .FirstOrDefaultAsync(cancellationToken);

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
}
