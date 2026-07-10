using System.Text.RegularExpressions;

namespace Blackwing.Persistence.Gallery;

/// <summary>
/// Canonical form used to enforce that a tag label is unique within a user and
/// type. Two values that differ only in case or surrounding/collapsible
/// whitespace normalize to the same key, so they resolve to one reusable tag
/// rather than many near-duplicates.
/// </summary>
public static partial class TagNormalization
{
    public static string Normalize(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var collapsed = Whitespace().Replace(value.Trim(), " ");
        return collapsed.ToUpperInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
