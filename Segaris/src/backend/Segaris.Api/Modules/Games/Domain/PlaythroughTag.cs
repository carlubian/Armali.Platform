namespace Segaris.Api.Modules.Games.Domain;

internal sealed class PlaythroughTag
{
    private PlaythroughTag()
    {
    }

    public int Id { get; private set; }
    public int PlaythroughId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string NormalizedValue { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static PlaythroughTag Create(int playthroughId, string value, int sortOrder)
    {
        GamesValidation.EnsurePositiveIdentifier(playthroughId, "playthroughId");
        var display = value.Trim();
        if (display.Length > GamesDefaults.TagMaximumLength)
        {
            throw new GamesValidationException(
                $"Tags may contain at most {GamesDefaults.TagMaximumLength} characters.",
                field: "tags");
        }

        return new PlaythroughTag
        {
            PlaythroughId = playthroughId,
            Value = display,
            NormalizedValue = display.ToUpperInvariant(),
            SortOrder = sortOrder,
        };
    }
}
