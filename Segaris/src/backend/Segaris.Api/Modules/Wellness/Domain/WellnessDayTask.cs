namespace Segaris.Api.Modules.Wellness.Domain;

/// <summary>
/// Snapshot of a catalogue task selected for one user day. The copied name and
/// category remain stable even if the catalogue row is later deleted.
/// </summary>
internal sealed class WellnessDayTask
{
    private WellnessDayTask()
    {
    }

    public int Id { get; private set; }
    public int WellnessDayId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WellnessCategory Category { get; private set; }
    public bool Completed { get; private set; }
    public int Position { get; private set; }

    public static WellnessDayTask CreateSnapshot(
        int wellnessDayId,
        string? name,
        WellnessCategory category,
        int position,
        bool completed = WellnessDefaults.TaskCompleted)
    {
        WellnessValidation.EnsurePositiveIdentifier(wellnessDayId, "wellnessDayId");
        if (position < 0)
        {
            throw new WellnessValidationException("Position cannot be negative.", "position");
        }

        return new WellnessDayTask
        {
            WellnessDayId = wellnessDayId,
            Name = WellnessValidation.ValidateTaskName(name),
            Category = WellnessValidation.ValidateCategory(category),
            Completed = completed,
            Position = position,
        };
    }

    public void SetCompletion(bool completed)
    {
        Completed = completed;
    }
}
