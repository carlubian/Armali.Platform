using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Wellness.Domain;

/// <summary>
/// One private Wellness day for a user and household date. The score is denormalized
/// so range reads do not need to load task snapshots.
/// </summary>
internal sealed class WellnessDay
{
    private WellnessDay()
    {
    }

    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int? Score { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static WellnessDay Create(DateOnly date, UserId ownerId, DateTimeOffset now, int? score = null)
    {
        WellnessValidation.EnsureUtc(now);
        var validatedScore = WellnessValidation.ValidateScore(score);

        return new WellnessDay
        {
            Date = date,
            Score = validatedScore,
            CreatedAt = now,
            CreatedBy = ownerId.Value,
            UpdatedAt = now,
            UpdatedBy = ownerId.Value,
        };
    }

    public void SetScore(int? score, UserId actorId, DateTimeOffset now)
    {
        WellnessValidation.EnsureUtc(now);
        Score = WellnessValidation.ValidateScore(score);
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
