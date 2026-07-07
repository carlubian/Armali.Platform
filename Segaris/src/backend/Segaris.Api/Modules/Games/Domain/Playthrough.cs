using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Games.Domain;

internal sealed record PlaythroughValues(
    string? Name,
    int GameId,
    int? StartYear,
    int? StartMonth,
    string? Status,
    IReadOnlyList<string>? Tags,
    string? Visibility);

internal sealed class Playthrough
{
    private Playthrough()
    {
    }

    public int Id { get; private set; }
    public int GameId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public int StartYear { get; private set; }
    public int StartMonth { get; private set; }
    public PlaythroughStatus Status { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Playthrough Create(PlaythroughValues values, UserId creatorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        var playthrough = new Playthrough
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        playthrough.Apply(values, creatorId, now, isCreation: true);
        return playthrough;
    }

    public void Update(PlaythroughValues values, UserId actorId, DateTimeOffset now) =>
        Apply(values, actorId, now, isCreation: false);

    internal void ReplaceGame(int gameId, UserId actorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        GamesValidation.EnsurePositiveIdentifier(gameId, "gameId");
        GameId = gameId;
        StampModification(actorId, now);
    }

    internal void MarkStructureChanged(UserId actorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        StampModification(actorId, now);
    }

    private void Apply(PlaythroughValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        GamesValidation.EnsureUtc(now);
        GamesValidation.EnsurePositiveIdentifier(values.GameId, "gameId");

        var visibility = GamesValidation.ValidateVisibility(values.Visibility);
        if (!isCreation && visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new GamesValidationException(
                "Only the creator may change playthrough visibility.",
                GamesValidationReason.VisibilityForbidden,
                "visibility");
        }

        Name = GamesValidation.ValidateName(values.Name);
        NormalizedName = GamesValidation.NormalizeName(Name);
        GameId = values.GameId;
        StartYear = GamesValidation.ValidateStartYear(values.StartYear);
        StartMonth = GamesValidation.ValidateStartMonth(values.StartMonth);
        Status = GamesValidation.ValidateStatus(values.Status);
        Visibility = visibility;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
