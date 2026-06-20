using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects.Domain;

internal sealed record ProjectRiskValues(
    string? Description,
    int Probability,
    int Impact,
    int Mitigation);

internal sealed class ProjectRisk
{
    private ProjectRisk()
    {
    }

    public int Id { get; private set; }
    public int ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Probability { get; private set; }
    public int Impact { get; private set; }
    public int Mitigation { get; private set; }
    public int Score { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static ProjectRisk Create(int projectId, ProjectRiskValues values, UserId creatorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsurePositiveIdentifier(projectId, "Project identifier");
        var risk = new ProjectRisk
        {
            ProjectId = projectId,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        risk.Apply(values, creatorId, now);
        return risk;
    }

    public void Update(ProjectRiskValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now);

    public RiskBand Band => ProjectRiskScoring.BandFor(Score);

    private void Apply(ProjectRiskValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        ProjectsValidation.EnsureUtc(now);
        ProjectsValidation.EnsureRiskFactorInRange(values.Probability, nameof(values.Probability));
        ProjectsValidation.EnsureRiskFactorInRange(values.Impact, nameof(values.Impact));
        ProjectsValidation.EnsureRiskFactorInRange(values.Mitigation, nameof(values.Mitigation));

        Description = ProjectsValidation.ValidateRiskDescription(values.Description);
        Probability = values.Probability;
        Impact = values.Impact;
        Mitigation = values.Mitigation;
        Score = ProjectRiskScoring.Score(Probability, Impact, Mitigation);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
