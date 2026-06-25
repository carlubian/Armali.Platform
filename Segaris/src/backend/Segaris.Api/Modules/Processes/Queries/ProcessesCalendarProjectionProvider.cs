using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes.Queries;

/// <summary>
/// Publishes due dates for pending steps that carry a step due date inside the requested
/// range. The process global due date is never published; resolved steps, cancelled
/// processes, and completed processes are excluded. A process with several qualifying
/// pending steps contributes one entry per step. Completed processes are implicitly
/// excluded because a completed process has no pending steps.
/// </summary>
internal sealed class ProcessesCalendarProjectionProvider(SegarisDbContext database)
    : IProcessesCalendarProjectionProvider
{
    public async Task<IReadOnlyList<ProcessStepDueCalendarProjection>> ListCalendarPendingStepDueDatesAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var accessibleProcesses = database.Set<Process>()
            .Where(ProcessPolicies.AccessibleTo(viewer))
            .Where(process => !process.IsCancelled);

        return await database.Set<Step>()
            .AsNoTracking()
            .Where(step => step.State == StepExecutionState.Pending)
            .Where(step => step.DueDate != null && step.DueDate >= from && step.DueDate <= to)
            .Join(
                accessibleProcesses,
                step => step.ProcessId,
                process => process.Id,
                (step, process) => new ProcessStepDueCalendarProjection(
                    process.Id,
                    step.Id,
                    process.Name,
                    step.Description,
                    step.DueDate!.Value,
                    $"/processes?processId={process.Id}&steps=true"))
            .ToArrayAsync(cancellationToken);
    }
}
