using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Maintenance.Contracts;
using Segaris.Api.Modules.Maintenance.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Maintenance.Mutations;

/// <summary>
/// Write-side operations on Maintenance tasks. Inaccessible tasks are reported as not
/// found so private records are never disclosed.
/// </summary>
internal sealed class MaintenanceTaskWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateMaintenanceTaskRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Title,
            request.MaintenanceTypeId,
            request.Status,
            request.Priority,
            request.DueDate,
            request.Notes,
            request.AssetId,
            request.Visibility);
        await ValidateTypeAsync(values.MaintenanceTypeId, cancellationToken);

        var task = MaintenanceTask.Create(
            values,
            actorId,
            clock.UtcNow,
            MaintenanceCivilDate.Today(clock));
        database.Add(task);
        await database.SaveChangesAsync(cancellationToken);
        return task.Id;
    }

    public async Task<bool> UpdateAsync(
        int taskId,
        UpdateMaintenanceTaskRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = await database.Set<MaintenanceTask>()
            .Where(MaintenanceTaskPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == taskId)
            .FirstOrDefaultAsync(cancellationToken);
        if (task is null)
        {
            return false;
        }

        var values = Map(
            request.Title,
            request.MaintenanceTypeId,
            request.Status,
            request.Priority,
            request.DueDate,
            request.Notes,
            request.AssetId,
            request.Visibility);
        await ValidateTypeAsync(values.MaintenanceTypeId, cancellationToken);

        if (values.Visibility != task.Visibility && !MaintenanceTaskPolicies.CanChangeVisibility(task, actorId))
        {
            throw new MaintenanceValidationException(
                "Only the creator may change task visibility.",
                MaintenanceValidationReason.VisibilityForbidden);
        }

        task.Update(values, actorId, clock.UtcNow, MaintenanceCivilDate.Today(clock));
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int taskId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var task = await database.Set<MaintenanceTask>()
            .Where(MaintenanceTaskPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == taskId)
            .FirstOrDefaultAsync(cancellationToken);
        if (task is null)
        {
            return false;
        }

        database.Remove(task);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateTypeAsync(int maintenanceTypeId, CancellationToken cancellationToken)
    {
        if (maintenanceTypeId <= 0)
        {
            throw new MaintenanceValidationException("Catalogue identifiers must be positive.");
        }

        if (!await database.Set<MaintenanceType>().AnyAsync(type => type.Id == maintenanceTypeId, cancellationToken))
        {
            throw new MaintenanceValidationException(
                "The maintenance type does not exist.",
                MaintenanceValidationReason.UnknownType);
        }
    }

    private static MaintenanceTaskValues Map(
        string? title,
        int maintenanceTypeId,
        string? status,
        string? priority,
        DateOnly? dueDate,
        string? notes,
        int? assetId,
        string? visibility) => new(
            title ?? string.Empty,
            maintenanceTypeId,
            ParseEnum(status, MaintenanceDefaults.Status, "status"),
            ParseEnum(priority, MaintenanceDefaults.Priority, "priority"),
            dueDate,
            notes,
            assetId,
            ParseEnum(visibility, MaintenanceDefaults.Visibility, "visibility"));

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new MaintenanceValidationException($"The {field} is not a recognized value.");
    }
}
