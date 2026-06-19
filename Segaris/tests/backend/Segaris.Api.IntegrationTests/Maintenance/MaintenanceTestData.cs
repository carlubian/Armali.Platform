using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Maintenance;

internal static class MaintenanceTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly SeedToday = new(2026, 1, 1);

    public static async Task<int> TypeIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<MaintenanceType>()
            .Where(type => type.Name == name)
            .Select(type => type.Id)
            .SingleAsync();
    }

    public static async Task<bool> TaskExistsAsync(IServiceProvider services, int taskId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<MaintenanceTask>().AnyAsync(task => task.Id == taskId);
    }

    public static async Task<int> SeedTaskAsync(
        IServiceProvider services,
        int creatorId,
        string title = "Maintenance task",
        string typeName = "Repair",
        MaintenanceStatus status = MaintenanceStatus.Pending,
        MaintenancePriority priority = MaintenancePriority.Medium,
        DateOnly? dueDate = null,
        string? notes = null,
        int? assetId = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var typeId = await database.Set<MaintenanceType>()
            .Where(type => type.Name == typeName)
            .Select(type => type.Id)
            .SingleAsync();

        var task = MaintenanceTask.Create(
            new MaintenanceTaskValues(
                title,
                typeId,
                status,
                priority,
                dueDate,
                notes,
                assetId,
                visibility),
            new UserId(creatorId),
            SeedNow,
            SeedToday);

        database.Add(task);
        await database.SaveChangesAsync();
        return task.Id;
    }
}
