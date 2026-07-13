using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Wellness.Domain;

/// <summary>
/// Administrator-managed, household-shared Wellness task catalogue row.
/// </summary>
internal sealed class WellnessTask
{
    private WellnessTask()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WellnessCategory Category { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }

    public static WellnessTask Create(
        string? name,
        WellnessCategory category,
        int sortOrder,
        UserId? creatorId,
        DateTimeOffset now)
    {
        WellnessValidation.EnsureUtc(now);
        if (sortOrder < 0)
        {
            throw new WellnessValidationException("Sort order cannot be negative.", "sortOrder");
        }

        return new WellnessTask
        {
            Name = WellnessValidation.ValidateTaskName(name),
            Category = WellnessValidation.ValidateCategory(category),
            SortOrder = sortOrder,
            CreatedAt = now,
            CreatedBy = creatorId?.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId?.Value,
        };
    }
}
