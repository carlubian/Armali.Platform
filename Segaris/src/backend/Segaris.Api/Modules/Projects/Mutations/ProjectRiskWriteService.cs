using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Projects.Mutations;

internal sealed class ProjectRiskWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateRiskAsync(
        int projectId,
        ProjectRiskRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RejectClientSuppliedScore(request);

        if (!await ProjectIsMutableByAsync(projectId, actorId, cancellationToken))
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        var risk = ProjectRisk.Create(projectId, Map(request), actorId, clock.UtcNow);
        database.Add(risk);
        await database.SaveChangesAsync(cancellationToken);
        return risk.Id;
    }

    public async Task<bool> UpdateRiskAsync(
        int projectId,
        int riskId,
        ProjectRiskRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RejectClientSuppliedScore(request);

        if (!await ProjectIsMutableByAsync(projectId, actorId, cancellationToken))
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        var risk = await database.Set<ProjectRisk>()
            .Where(candidate => candidate.ProjectId == projectId)
            .Where(candidate => candidate.Id == riskId)
            .FirstOrDefaultAsync(cancellationToken);
        if (risk is null)
        {
            return false;
        }

        risk.Update(Map(request), actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteRiskAsync(
        int projectId,
        int riskId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectIsMutableByAsync(projectId, actorId, cancellationToken))
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        var risk = await database.Set<ProjectRisk>()
            .Where(candidate => candidate.ProjectId == projectId)
            .Where(candidate => candidate.Id == riskId)
            .FirstOrDefaultAsync(cancellationToken);
        if (risk is null)
        {
            return false;
        }

        database.Remove(risk);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<bool> ProjectIsMutableByAsync(int projectId, UserId actorId, CancellationToken cancellationToken) =>
        database.Set<Project>()
            .Where(ProjectItemPolicies.MutableBy<Project>(actorId))
            .AnyAsync(project => project.Id == projectId, cancellationToken);

    private static ProjectRiskValues Map(ProjectRiskRequest request) => new(
        request.Description,
        request.Probability,
        request.Impact,
        request.Mitigation);

    private static void RejectClientSuppliedScore(ProjectRiskRequest request)
    {
        if (request.ExtensionData?.Keys.Any(key => string.Equals(key, "score", StringComparison.OrdinalIgnoreCase)) == true)
        {
            throw new ProjectsValidationException("Risk score is system-computed and must not be supplied by clients.");
        }
    }
}
