using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Wellness.Contracts;
using Segaris.Api.Modules.Wellness.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Wellness.Queries;

internal sealed class WellnessDayService(
    SegarisDbContext database,
    IClock clock,
    WellnessDaySelector selector)
{
    public async Task<WellnessTodayResponse> GetTodayAsync(UserId owner, CancellationToken cancellationToken)
    {
        var today = WellnessCivilDate.Today(clock);
        var existing = await FindDayAsync(owner, today, track: false, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        try
        {
            return await GenerateTodayAsync(owner, today, cancellationToken);
        }
        catch (DbUpdateException)
        {
            database.ChangeTracker.Clear();
            var concurrentlyCreated = await FindDayAsync(owner, today, track: false, cancellationToken);
            if (concurrentlyCreated is not null)
            {
                return concurrentlyCreated;
            }

            throw;
        }
    }

    public async Task<WellnessTodayResponse> ToggleTaskAsync(
        int dayTaskId,
        UserId owner,
        CancellationToken cancellationToken)
    {
        var today = WellnessCivilDate.Today(clock);
        var dayTask = await database.Set<WellnessDayTask>()
            .SingleOrDefaultAsync(task => task.Id == dayTaskId, cancellationToken)
            ?? throw WellnessProblem.DayTaskNotFound();

        var day = await database.Set<WellnessDay>()
            .SingleOrDefaultAsync(existing => existing.Id == dayTask.WellnessDayId
                && existing.CreatedBy == owner.Value
                && existing.Date == today, cancellationToken)
            ?? throw WellnessProblem.DayTaskNotFound();

        dayTask.SetCompletion(!dayTask.Completed);

        var tasks = await database.Set<WellnessDayTask>()
            .Where(task => task.WellnessDayId == day.Id)
            .ToListAsync(cancellationToken);
        day.SetScore(WellnessScore.Compute(tasks.Count(task => task.Completed), tasks.Count), owner, clock.UtcNow);

        await database.SaveChangesAsync(cancellationToken);
        return ToToday(day, tasks);
    }

    public async Task<WellnessDayListResponse> ListDaysAsync(
        DateOnly? from,
        DateOnly? to,
        UserId owner,
        CancellationToken cancellationToken)
    {
        if (from is null || to is null || from.Value > to.Value)
        {
            throw WellnessProblem.DayRangeValidation();
        }

        var days = await database.Set<WellnessDay>()
            .AsNoTracking()
            .Where(day => day.CreatedBy == owner.Value && day.Date >= from && day.Date <= to)
            .OrderBy(day => day.Date)
            .ThenBy(day => day.Id)
            .Select(day => new WellnessDayScoreResponse(day.Date, day.Score))
            .ToListAsync(cancellationToken);

        return new WellnessDayListResponse(from.Value, to.Value, days);
    }

    private async Task<WellnessTodayResponse> GenerateTodayAsync(
        UserId owner,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        var catalogue = await database.Set<WellnessTask>()
            .AsNoTracking()
            .OrderBy(task => task.SortOrder)
            .ThenBy(task => task.Id)
            .Select(task => new WellnessSelectableTask(task.Id, task.Name, task.Category))
            .ToListAsync(cancellationToken);

        var selected = selector.Select(catalogue);
        var day = WellnessDay.Create(today, owner, clock.UtcNow, WellnessScore.Compute(0, selected.Count));
        database.Add(day);
        await database.SaveChangesAsync(cancellationToken);

        var tasks = selected
            .Select((task, index) => WellnessDayTask.CreateSnapshot(day.Id, task.Name, task.Category, index))
            .ToArray();
        database.AddRange(tasks);
        await database.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return ToToday(day, tasks);
    }

    private async Task<WellnessTodayResponse?> FindDayAsync(
        UserId owner,
        DateOnly date,
        bool track,
        CancellationToken cancellationToken)
    {
        var days = database.Set<WellnessDay>().Where(day => day.CreatedBy == owner.Value && day.Date == date);
        if (!track)
        {
            days = days.AsNoTracking();
        }

        var day = await days.SingleOrDefaultAsync(cancellationToken);
        if (day is null)
        {
            return null;
        }

        var tasks = await database.Set<WellnessDayTask>()
            .AsNoTracking()
            .Where(task => task.WellnessDayId == day.Id)
            .OrderBy(task => task.Position)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);

        return ToToday(day, tasks);
    }

    private static WellnessTodayResponse ToToday(WellnessDay day, IReadOnlyList<WellnessDayTask> tasks) =>
        new(
            day.Date,
            day.Score,
            tasks.OrderBy(task => task.Position)
                .ThenBy(task => task.Id)
                .Select(task => new WellnessDayTaskResponse(
                    task.Id,
                    task.Name,
                    task.Category.ToString(),
                    task.Completed,
                    task.Position))
                .ToArray());
}
