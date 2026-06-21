using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Processes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Processes;

internal static class ProcessTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<ProcessCategory>()
            .Where(category => category.Name == name)
            .Select(category => category.Id)
            .SingleAsync();
    }

    public static async Task<bool> ProcessExistsAsync(IServiceProvider services, int processId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Process>().AnyAsync(process => process.Id == processId);
    }

    public static async Task<int> SeedProcessAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Household process",
        string categoryName = "Administrative",
        DateOnly? dueDate = null,
        string? notes = null,
        bool isCancelled = false,
        RecordVisibility visibility = RecordVisibility.Public,
        IReadOnlyList<SeedStep>? steps = null)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var categoryId = await database.Set<ProcessCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();
        var actor = new UserId(creatorId);
        var process = Process.Create(
            new ProcessValues(name, categoryId, dueDate, notes, visibility),
            actor,
            SeedNow);
        if (isCancelled)
        {
            process.Cancel(actor, SeedNow.AddMinutes(1));
        }

        database.Add(process);
        await database.SaveChangesAsync();

        if (steps is not null)
        {
            for (var index = 0; index < steps.Count; index++)
            {
                var seed = steps[index];
                var step = Step.Create(
                    process.Id,
                    new StepValues(seed.Description, seed.DueDate, seed.Notes, seed.IsOptional),
                    index,
                    actor,
                    SeedNow);
                if (seed.State == StepExecutionState.Completed)
                {
                    step.Complete(actor, SeedNow.AddMinutes(2 + index));
                }
                else if (seed.State == StepExecutionState.Skipped)
                {
                    step.Skip(actor, SeedNow.AddMinutes(2 + index));
                }

                database.Add(step);
            }

            await database.SaveChangesAsync();
        }

        return process.Id;
    }
}

internal sealed record SeedStep(
    string Description,
    DateOnly? DueDate = null,
    string? Notes = null,
    bool IsOptional = false,
    StepExecutionState State = StepExecutionState.Pending);
