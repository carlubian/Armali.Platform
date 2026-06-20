using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>
/// A public Projects axis under a required program. Axes are identified by a globally
/// unique four-letter code and own projects and activities as leaf children.
/// </summary>
internal sealed class ProjectAxis
{
    private ProjectAxis()
    {
    }

    public int Id { get; private set; }
    public int ProgramId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static ProjectAxis Create(
        int programId,
        string? name,
        string? code,
        UserId creatorId,
        DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        var axis = new ProjectAxis
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        axis.Update(programId, name, code, creatorId, now);
        axis.CreatedAt = now;
        axis.CreatedBy = creatorId.Value;
        return axis;
    }

    public void Update(int programId, string? name, string? code, UserId actorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        ProjectsValidation.EnsurePositiveIdentifier(programId, "Program identifier");
        ProgramId = programId;
        Name = ProjectsValidation.ValidateName(name);
        Code = ProjectsValidation.ValidateCode(code);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    internal void ReplaceProgram(int programId, UserId actorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        ProjectsValidation.EnsurePositiveIdentifier(programId, "Program identifier");
        ProgramId = programId;
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
