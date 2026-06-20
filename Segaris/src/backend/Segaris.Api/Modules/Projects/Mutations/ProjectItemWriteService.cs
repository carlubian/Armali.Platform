using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Projects.Mutations;

internal sealed class ProjectItemWriteService(
    SegarisDbContext database,
    ProjectNumberAllocator numberAllocator,
    IClock clock,
    IAttachmentService attachments)
{
    public async Task<int> CreateProjectAsync(
        CreateProjectRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureAxisExistsAsync(request.AxisId, cancellationToken);
        var values = Map(request.AxisId, request.Name, request.Status, request.Visibility);
        var number = await numberAllocator.AllocateAsync(cancellationToken);
        var project = Project.Create(values, number, actorId, clock.UtcNow);
        database.Add(project);
        await database.SaveChangesAsync(cancellationToken);
        return project.Id;
    }

    public async Task<bool> UpdateProjectAsync(
        int projectId,
        UpdateProjectRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var project = await database.Set<Project>()
            .Where(ProjectItemPolicies.MutableBy<Project>(actorId))
            .Where(candidate => candidate.Id == projectId)
            .FirstOrDefaultAsync(cancellationToken);
        if (project is null)
        {
            return false;
        }

        await EnsureAxisExistsAsync(request.AxisId, cancellationToken);
        project.Update(Map(request.AxisId, request.Name, request.Status, request.Visibility), actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteProjectAsync(
        int projectId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var project = await database.Set<Project>()
            .Where(ProjectItemPolicies.MutableBy<Project>(actorId))
            .Where(candidate => candidate.Id == projectId)
            .FirstOrDefaultAsync(cancellationToken);
        if (project is null)
        {
            return false;
        }

        database.Remove(project);
        await database.SaveChangesAsync(cancellationToken);

        var owner = ProjectsAttachments.ProjectOwner(projectId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    public async Task<int> CreateActivityAsync(
        CreateActivityRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureAxisExistsAsync(request.AxisId, cancellationToken);
        var values = Map(request.AxisId, request.Name, request.Status, request.Visibility);
        var number = await numberAllocator.AllocateAsync(cancellationToken);
        var activity = Activity.Create(values, number, actorId, clock.UtcNow);
        database.Add(activity);
        await database.SaveChangesAsync(cancellationToken);
        return activity.Id;
    }

    public async Task<bool> UpdateActivityAsync(
        int activityId,
        UpdateActivityRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var activity = await database.Set<Activity>()
            .Where(ProjectItemPolicies.MutableBy<Activity>(actorId))
            .Where(candidate => candidate.Id == activityId)
            .FirstOrDefaultAsync(cancellationToken);
        if (activity is null)
        {
            return false;
        }

        await EnsureAxisExistsAsync(request.AxisId, cancellationToken);
        activity.Update(Map(request.AxisId, request.Name, request.Status, request.Visibility), actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteActivityAsync(
        int activityId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var activity = await database.Set<Activity>()
            .Where(ProjectItemPolicies.MutableBy<Activity>(actorId))
            .Where(candidate => candidate.Id == activityId)
            .FirstOrDefaultAsync(cancellationToken);
        if (activity is null)
        {
            return false;
        }

        database.Remove(activity);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureAxisExistsAsync(int axisId, CancellationToken cancellationToken)
    {
        if (axisId <= 0 || !await database.Set<ProjectAxis>().AnyAsync(axis => axis.Id == axisId, cancellationToken))
        {
            throw ProjectsStructureProblem.AxisNotFound();
        }
    }

    private static ProjectItemValues Map(
        int axisId,
        string? name,
        string? status,
        string? visibility) => new(
        axisId,
        name,
        ParseEnum(status, ProjectsDefaults.Status, "status"),
        ParseEnum(visibility, ProjectsDefaults.Visibility, "visibility"));

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (value is null)
        {
            return defaultValue;
        }

        if (string.IsNullOrWhiteSpace(value)
            || !Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new ProjectsValidationException($"The {field} is not a recognized value.");
        }

        return parsed;
    }
}
