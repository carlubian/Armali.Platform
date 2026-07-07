using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Games.Domain;

internal sealed class Section
{
    private Section()
    {
    }

    public int Id { get; private set; }
    public int PlaythroughId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public SectionColor Color { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static Section Create(
        int playthroughId,
        string? name,
        string? color,
        int sortOrder,
        UserId creatorId,
        DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        GamesValidation.EnsurePositiveIdentifier(playthroughId, "playthroughId");
        var section = new Section
        {
            PlaythroughId = playthroughId,
            SortOrder = sortOrder,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        section.Update(name, color, creatorId, now);
        return section;
    }

    public void Update(string? name, string? color, UserId actorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        Name = GamesValidation.ValidateName(name);
        NormalizedName = GamesValidation.NormalizeName(Name);
        Color = GamesValidation.ValidateColor(color);
        StampModification(actorId, now);
    }

    public void Reposition(int sortOrder, UserId actorId, DateTimeOffset now)
    {
        GamesValidation.EnsureUtc(now);
        SortOrder = sortOrder;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }
}
