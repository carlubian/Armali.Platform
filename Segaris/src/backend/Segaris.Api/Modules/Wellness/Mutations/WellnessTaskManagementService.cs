using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Wellness.Mutations;

/// <summary>
/// Administrator create and delete of the shared task catalogue, surfaced through the
/// Configuration presentation boundary. Tasks are created or deleted only: there is no
/// update, move, or replacement flow. Deletion is impact-free because in-progress and
/// past days hold independent <c>WellnessDayTask</c> snapshots, so removing a catalogue
/// task never alters a persisted day.
/// </summary>
internal sealed class WellnessTaskManagementService(SegarisDbContext database, IClock clock)
{
    public async Task<WellnessTaskResponse> CreateAsync(
        CreateWellnessTaskRequest request,
        UserId? actor,
        CancellationToken cancellationToken)
    {
        WellnessTask task;
        try
        {
            var category = WellnessValidation.ParseCategory(request.Category);
            var sortOrder = (await database.Set<WellnessTask>()
                .Select(existing => (int?)existing.SortOrder)
                .MaxAsync(cancellationToken) ?? -1) + 1;
            task = WellnessTask.Create(request.Name, category, sortOrder, actor, clock.UtcNow);
        }
        catch (WellnessValidationException exception)
        {
            throw WellnessProblem.TaskValidation(exception.Field ?? "name", exception.Message);
        }

        database.Add(task);
        await database.SaveChangesAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await database.Set<WellnessTask>()
            .SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken)
            ?? throw WellnessProblem.TaskNotFound();

        database.Remove(task);
        await database.SaveChangesAsync(cancellationToken);
    }

    private static WellnessTaskResponse ToResponse(WellnessTask task) =>
        new(task.Id, task.Name, task.Category.ToString(), task.SortOrder);
}
