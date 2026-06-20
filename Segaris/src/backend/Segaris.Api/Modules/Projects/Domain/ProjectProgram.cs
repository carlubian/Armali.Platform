using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>
/// A public top-level Projects structural node. Programs are identified by a globally
/// unique four-letter code and own axes as their required children.
/// </summary>
internal sealed class ProjectProgram
{
    private ProjectProgram()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static ProjectProgram Create(string? name, string? code, UserId creatorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        var program = new ProjectProgram
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        program.Update(name, code, creatorId, now);
        program.CreatedAt = now;
        program.CreatedBy = creatorId.Value;
        return program;
    }

    public void Update(string? name, string? code, UserId actorId, DateTimeOffset now)
    {
        ProjectsValidation.EnsureUtc(now);
        Name = ProjectsValidation.ValidateName(name);
        Code = ProjectsValidation.ValidateCode(code);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
