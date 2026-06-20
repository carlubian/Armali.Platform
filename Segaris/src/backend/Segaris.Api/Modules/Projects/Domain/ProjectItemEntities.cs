using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects.Domain;

internal sealed record ProjectItemValues(
    int AxisId,
    string? Name,
    ProjectStatus Status,
    RecordVisibility Visibility);

internal abstract class ProjectItem
{
    public int Id { get; private set; }
    public int AxisId { get; private set; }
    public int Number { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ProjectStatus Status { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    protected void Initialize(ProjectItemValues values, int number, UserId creatorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        ProjectsValidation.EnsurePositiveIdentifier(number, "Project number");
        CreatedAt = now;
        CreatedBy = creatorId.Value;
        UpdatedAt = now;
        UpdatedBy = creatorId.Value;
        Number = number;
        Apply(values, creatorId, now, isCreation: true);
    }

    public void Update(ProjectItemValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now, isCreation: false);

    internal void ReplaceAxis(int axisId, UserId actorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        ProjectsValidation.EnsurePositiveIdentifier(axisId, "Axis identifier");
        AxisId = axisId;
        StampModification(actorId, now);
    }

    public string Identifier(string programCode, string axisCode) =>
        ProjectIdentifier.Format(programCode, axisCode, Number, Name);

    private void Apply(ProjectItemValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        ProjectsValidation.EnsureUtc(now);
        ProjectsValidation.EnsurePositiveIdentifier(values.AxisId, "Axis identifier");
        ProjectsValidation.EnsureKnownStatusAndVisibility(values.Status, values.Visibility);

        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new ProjectsValidationException(
                "Only the creator may change visibility.",
                ProjectsValidationReason.VisibilityForbidden);
        }

        AxisId = values.AxisId;
        Name = ProjectsValidation.ValidateName(values.Name);
        Status = values.Status;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}

internal sealed class Project : ProjectItem
{
    private Project()
    {
    }

    public static Project Create(ProjectItemValues values, int number, UserId creatorId, DateTimeOffset now)
    {
        var project = new Project();
        project.Initialize(values, number, creatorId, now);
        return project;
    }
}

internal sealed class Activity : ProjectItem
{
    private Activity()
    {
    }

    public static Activity Create(ProjectItemValues values, int number, UserId creatorId, DateTimeOffset now)
    {
        var activity = new Activity();
        activity.Initialize(values, number, creatorId, now);
        return activity;
    }
}
