using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Processes.Attention;

/// <summary>
/// Contributes the Processes launcher card's attention state. Attention is required
/// when the current user can access at least one open process whose global due date,
/// or otherwise next pending frontier step due date, is overdue or due through the
/// next 7 natural days in <c>Europe/Madrid</c>.
/// </summary>
internal sealed class ProcessesAttentionContributor(
    SegarisDbContext database,
    ICurrentUser currentUser,
    IClock clock) : ILauncherAttentionContributor
{
    public string Module => ProcessesLauncherCard.ModuleKey;

    public async Task<bool> RequiresAttentionAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        var windowEnd = ProcessCivilDate.Today(clock).AddDays(7);

        return await database.Set<Process>()
            .AsNoTracking()
            .Where(ProcessPolicies.AccessibleTo(userId))
            .Where(process => !process.IsCancelled)
            .Select(process => new
            {
                process.DueDate,
                HasAnyStep = database.Set<Step>().Any(step => step.ProcessId == process.Id),
                HasPendingStep = database.Set<Step>().Any(step => step.ProcessId == process.Id && step.State == StepExecutionState.Pending),
                FrontierStepDueDate = database.Set<Step>()
                    .Where(step => step.ProcessId == process.Id && step.State == StepExecutionState.Pending)
                    .OrderBy(step => step.SortOrder)
                    .ThenBy(step => step.Id)
                    .Select(step => step.DueDate)
                    .FirstOrDefault(),
            })
            .Where(process => !process.HasAnyStep || process.HasPendingStep)
            .AnyAsync(
                process => (process.DueDate.HasValue && process.DueDate.Value <= windowEnd)
                    || (!process.DueDate.HasValue
                        && process.FrontierStepDueDate.HasValue
                        && process.FrontierStepDueDate.Value <= windowEnd),
                cancellationToken);
    }
}
