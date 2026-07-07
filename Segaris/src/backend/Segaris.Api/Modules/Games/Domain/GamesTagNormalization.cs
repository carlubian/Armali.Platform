namespace Segaris.Api.Modules.Games.Domain;

/// <summary>
/// Frozen normalization rules for a playthrough's free-text tags. Tags are not
/// backed by a catalogue: on save each candidate is trimmed, empty values are
/// discarded, and duplicates are removed case-insensitively while preserving the
/// capitalization of the first kept value. The resulting order follows the first
/// appearance of each kept value.
/// </summary>
internal static class GamesTagNormalization
{
    /// <summary>The persisted maximum length of a single tag.</summary>
    public const int TagMaximumLength = GamesDefaults.TagMaximumLength;

    public static IReadOnlyList<string> Normalize(IEnumerable<string?>? tags)
    {
        if (tags is null)
        {
            return [];
        }

        var kept = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            var trimmed = tag?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                kept.Add(trimmed);
            }
        }

        return kept;
    }
}
