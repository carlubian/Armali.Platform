using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Processes.Contracts;

internal sealed record ProcessStepDueCalendarProjection(
    int ProcessId,
    int StepId,
    string ProcessTitle,
    string StepTitle,
    DateOnly DueDate,
    string? TargetRoute);

internal interface IProcessesCalendarProjectionProvider
{
    Task<IReadOnlyList<ProcessStepDueCalendarProjection>> ListCalendarPendingStepDueDatesAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
