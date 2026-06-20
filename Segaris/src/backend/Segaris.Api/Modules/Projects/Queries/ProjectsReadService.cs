using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Identity;
using Segaris.Api.Modules.Projects.Contracts;
using Segaris.Api.Modules.Projects.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects.Queries;

internal sealed class ProjectsReadService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<ProgramNodeResponse>> ListProgramsAsync(CancellationToken cancellationToken) =>
        await database.Set<ProjectProgram>()
            .AsNoTracking()
            .OrderBy(program => program.Code)
            .ThenBy(program => program.Id)
            .Select(program => new ProgramNodeResponse(program.Id, program.Code, program.Name))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<AxisNodeResponse>> ListAxesByProgramAsync(
        int programId,
        CancellationToken cancellationToken)
    {
        if (!await database.Set<ProjectProgram>().AsNoTracking().AnyAsync(program => program.Id == programId, cancellationToken))
        {
            throw ProjectsStructureProblem.ProgramNotFound();
        }

        return await database.Set<ProjectAxis>()
            .AsNoTracking()
            .Where(axis => axis.ProgramId == programId)
            .OrderBy(axis => axis.Code)
            .ThenBy(axis => axis.Id)
            .Select(axis => new AxisNodeResponse(axis.Id, axis.Code, axis.Name, axis.ProgramId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectTreeItemResponse>> ListItemsByAxisAsync(
        int axisId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var axis = await AxisRowAsync(axisId, cancellationToken);
        var projects = await database.Set<Project>()
            .AsNoTracking()
            .Where(ProjectItemPolicies.AccessibleTo<Project>(userId))
            .Where(project => project.AxisId == axisId)
            .Select(project => new ItemRow(
                project.Id,
                "Project",
                project.Number,
                project.Name,
                project.Status,
                project.Visibility,
                null))
            .ToArrayAsync(cancellationToken);
        var riskSummaries = await RiskSummariesByProjectAsync(projects.Select(project => project.Id), cancellationToken);
        var activities = await database.Set<Activity>()
            .AsNoTracking()
            .Where(ProjectItemPolicies.AccessibleTo<Activity>(userId))
            .Where(activity => activity.AxisId == axisId)
            .Select(activity => new ItemRow(
                activity.Id,
                "Activity",
                activity.Number,
                activity.Name,
                activity.Status,
                activity.Visibility,
                null))
            .ToArrayAsync(cancellationToken);

        return projects
            .Concat(activities)
            .OrderBy(item => item.Number)
            .ThenBy(item => item.Kind, StringComparer.Ordinal)
            .ThenBy(item => item.Id)
            .Select(item => item.ToTreeResponse(axis.ProgramCode, axis.AxisCode, riskSummaries))
            .ToArray();
    }

    public async Task<ProjectResponse?> GetProjectAsync(
        int projectId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Project>()
            .AsNoTracking()
            .Where(ProjectItemPolicies.AccessibleTo<Project>(userId))
            .Where(project => project.Id == projectId)
            .Select(project => new ProjectDetailRow(
                project.Id,
                project.Number,
                database.Set<ProjectProgram>()
                    .Where(program => program.Id == database.Set<ProjectAxis>()
                        .Where(axis => axis.Id == project.AxisId)
                        .Select(axis => axis.ProgramId)
                        .First())
                    .Select(program => program.Code)
                    .First(),
                database.Set<ProjectAxis>().Where(axis => axis.Id == project.AxisId).Select(axis => axis.Code).First(),
                project.Name,
                project.Status,
                project.Visibility,
                project.AxisId,
                project.CreatedBy,
                database.Set<SegarisUser>().Where(user => user.Id == project.CreatedBy).Select(user => user.DisplayName).First(),
                project.CreatedAt,
                project.UpdatedBy,
                database.Set<SegarisUser>().Where(user => user.Id == project.UpdatedBy).Select(user => user.DisplayName).First(),
                project.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var riskSummary = await RiskSummaryForProjectAsync(projectId, cancellationToken);
        return row.ToResponse(riskSummary);
    }

    public async Task<IReadOnlyList<ProjectRiskResponse>> ListRisksAsync(
        int projectId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectIsAccessibleAsync(projectId, userId, cancellationToken))
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        var risks = await database.Set<ProjectRisk>()
            .AsNoTracking()
            .Where(risk => risk.ProjectId == projectId)
            .OrderBy(risk => risk.Id)
            .Select(risk => new RiskRow(
                risk.Id,
                risk.Description,
                risk.Probability,
                risk.Impact,
                risk.Mitigation,
                risk.Score))
            .ToArrayAsync(cancellationToken);

        return risks.Select(static risk => risk.ToResponse()).ToArray();
    }

    public async Task<ProjectRiskResponse?> GetRiskAsync(
        int projectId,
        int riskId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectIsAccessibleAsync(projectId, userId, cancellationToken))
        {
            throw ProjectsProblem.ProjectNotFound();
        }

        var row = await database.Set<ProjectRisk>()
            .AsNoTracking()
            .Where(risk => risk.ProjectId == projectId)
            .Where(risk => risk.Id == riskId)
            .Select(risk => new RiskRow(
                risk.Id,
                risk.Description,
                risk.Probability,
                risk.Impact,
                risk.Mitigation,
                risk.Score))
            .FirstOrDefaultAsync(cancellationToken);

        return row?.ToResponse();
    }

    public async Task<ActivityResponse?> GetActivityAsync(
        int activityId,
        UserId userId,
        CancellationToken cancellationToken)
    {
        var row = await database.Set<Activity>()
            .AsNoTracking()
            .Where(ProjectItemPolicies.AccessibleTo<Activity>(userId))
            .Where(activity => activity.Id == activityId)
            .Select(activity => new ActivityDetailRow(
                activity.Id,
                activity.Number,
                database.Set<ProjectProgram>()
                    .Where(program => program.Id == database.Set<ProjectAxis>()
                        .Where(axis => axis.Id == activity.AxisId)
                        .Select(axis => axis.ProgramId)
                        .First())
                    .Select(program => program.Code)
                    .First(),
                database.Set<ProjectAxis>().Where(axis => axis.Id == activity.AxisId).Select(axis => axis.Code).First(),
                activity.Name,
                activity.Status,
                activity.Visibility,
                activity.AxisId,
                activity.CreatedBy,
                database.Set<SegarisUser>().Where(user => user.Id == activity.CreatedBy).Select(user => user.DisplayName).First(),
                activity.CreatedAt,
                activity.UpdatedBy,
                database.Set<SegarisUser>().Where(user => user.Id == activity.UpdatedBy).Select(user => user.DisplayName).First(),
                activity.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return row?.ToResponse();
    }

    private async Task<AxisCodesRow> AxisRowAsync(int axisId, CancellationToken cancellationToken) =>
        await database.Set<ProjectAxis>()
            .AsNoTracking()
            .Where(axis => axis.Id == axisId)
            .Select(axis => new AxisCodesRow(
                axis.Code,
                database.Set<ProjectProgram>().Where(program => program.Id == axis.ProgramId).Select(program => program.Code).First()))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw ProjectsStructureProblem.AxisNotFound();

    private sealed record AxisCodesRow(string AxisCode, string ProgramCode);

    private sealed record ItemRow(
        int Id,
        string Kind,
        int Number,
        string Name,
        ProjectStatus Status,
        Segaris.Shared.Authorization.RecordVisibility Visibility,
        ProjectRiskBandSummaryResponse? RiskSummary)
    {
        public ProjectTreeItemResponse ToTreeResponse(
            string programCode,
            string axisCode,
            IReadOnlyDictionary<int, ProjectRiskBandSummaryResponse> riskSummaries) => new(
            Id,
            Kind,
            Number,
            ProjectIdentifier.Format(programCode, axisCode, Number, Name),
            Name,
            Status.ToString(),
            Visibility.ToString(),
            Kind == "Project" ? riskSummaries.GetValueOrDefault(Id, EmptyRiskSummary) : RiskSummary);
    }

    private sealed record ProjectDetailRow(
        int Id,
        int Number,
        string ProgramCode,
        string AxisCode,
        string Name,
        ProjectStatus Status,
        Segaris.Shared.Authorization.RecordVisibility Visibility,
        int AxisId,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt)
    {
        public ProjectResponse ToResponse(ProjectRiskBandSummaryResponse riskSummary) => new(
            Id,
            Number,
            ProjectIdentifier.Format(ProgramCode, AxisCode, Number, Name),
            Name,
            Status.ToString(),
            Visibility.ToString(),
            AxisId,
            riskSummary,
            [],
            CreatedById,
            CreatedByName,
            CreatedAt,
            UpdatedById,
            UpdatedByName,
            UpdatedAt);
    }

    private sealed record ActivityDetailRow(
        int Id,
        int Number,
        string ProgramCode,
        string AxisCode,
        string Name,
        ProjectStatus Status,
        Segaris.Shared.Authorization.RecordVisibility Visibility,
        int AxisId,
        int CreatedById,
        string CreatedByName,
        DateTimeOffset CreatedAt,
        int UpdatedById,
        string UpdatedByName,
        DateTimeOffset UpdatedAt)
    {
        public ActivityResponse ToResponse() => new(
            Id,
            Number,
            ProjectIdentifier.Format(ProgramCode, AxisCode, Number, Name),
            Name,
            Status.ToString(),
            Visibility.ToString(),
            AxisId,
            CreatedById,
            CreatedByName,
            CreatedAt,
            UpdatedById,
            UpdatedByName,
            UpdatedAt);
    }

    private static readonly ProjectRiskBandSummaryResponse EmptyRiskSummary = new(0, 0, 0);

    private async Task<bool> ProjectIsAccessibleAsync(int projectId, UserId userId, CancellationToken cancellationToken) =>
        await database.Set<Project>()
            .AsNoTracking()
            .Where(ProjectItemPolicies.AccessibleTo<Project>(userId))
            .AnyAsync(project => project.Id == projectId, cancellationToken);

    private async Task<ProjectRiskBandSummaryResponse> RiskSummaryForProjectAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var summaries = await RiskSummariesByProjectAsync([projectId], cancellationToken);
        return summaries.GetValueOrDefault(projectId, EmptyRiskSummary);
    }

    private async Task<IReadOnlyDictionary<int, ProjectRiskBandSummaryResponse>> RiskSummariesByProjectAsync(
        IEnumerable<int> projectIds,
        CancellationToken cancellationToken)
    {
        var ids = projectIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<int, ProjectRiskBandSummaryResponse>();
        }

        var risks = await database.Set<ProjectRisk>()
            .AsNoTracking()
            .Where(risk => ids.Contains(risk.ProjectId))
            .Select(risk => new { risk.ProjectId, risk.Score })
            .ToArrayAsync(cancellationToken);

        return risks
            .GroupBy(risk => risk.ProjectId)
            .ToDictionary(
                group => group.Key,
                group => new ProjectRiskBandSummaryResponse(
                    group.Count(risk => ProjectRiskScoring.BandFor(risk.Score) == RiskBand.Low),
                    group.Count(risk => ProjectRiskScoring.BandFor(risk.Score) == RiskBand.Medium),
                    group.Count(risk => ProjectRiskScoring.BandFor(risk.Score) == RiskBand.High)));
    }

    private sealed record RiskRow(
        int Id,
        string Description,
        int Probability,
        int Impact,
        int Mitigation,
        int Score)
    {
        public ProjectRiskResponse ToResponse() => new(
            Id,
            Description,
            Probability,
            Impact,
            Mitigation,
            Score,
            ProjectRiskScoring.BandFor(Score).ToString());
    }
}
