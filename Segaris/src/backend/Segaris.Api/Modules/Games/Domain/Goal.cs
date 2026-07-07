using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Games.Domain;

internal sealed class Goal
{
    private Goal()
    {
    }

    public int Id { get; private set; }
    public int SectionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public int Position { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Goal Create(int sectionId, string? text, int position, UserId creatorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        GamesValidation.EnsurePositiveIdentifier(sectionId, "sectionId");
        var goal = new Goal
        {
            SectionId = sectionId,
            Completed = GamesDefaults.GoalCompleted,
            Position = position,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        goal.Update(text, creatorId, now);
        return goal;
    }

    public void Update(string? text, UserId actorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        Text = GamesValidation.ValidateGoalText(text);
        StampModification(actorId, now);
    }

    public void SetCompletion(bool completed, UserId actorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        Completed = completed;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
