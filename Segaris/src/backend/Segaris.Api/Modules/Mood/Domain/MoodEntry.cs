using Segaris.Api.Modules.Mood.Contracts;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Mood.Domain;

internal sealed record MoodEntryValues(
    DateOnly EntryDate,
    int Score,
    MoodEnergy Energy,
    MoodAlignment Alignment,
    MoodDirection Direction,
    MoodSource Source,
    string? Notes);

internal sealed class MoodEntry
{
    private MoodEntry()
    {
    }

    public int Id { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public int Score { get; private set; }
    public MoodEnergy Energy { get; private set; }
    public MoodAlignment Alignment { get; private set; }
    public MoodDirection Direction { get; private set; }
    public MoodSource Source { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }

    public static MoodEntry Create(MoodEntryValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var entry = new MoodEntry
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
        };
        entry.Apply(values);
        return entry;
    }

    public void Update(MoodEntryValues values, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        Apply(values);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private void Apply(MoodEntryValues values)
    {
        EntryDate = values.EntryDate;
        Score = MoodValidation.ValidateScore(values.Score);
        Energy = MoodValidation.ValidateEnergy(values.Energy);
        Alignment = MoodValidation.ValidateAlignment(values.Alignment);
        Direction = MoodValidation.ValidateDirection(values.Direction);
        Source = MoodValidation.ValidateSource(values.Source);
        Notes = MoodValidation.ValidateNotes(values.Notes);
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new MoodValidationException("Technical timestamps must use UTC.");
        }
    }
}
