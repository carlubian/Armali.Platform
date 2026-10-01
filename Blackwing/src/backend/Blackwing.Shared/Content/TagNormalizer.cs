using System.Globalization;
using System.Text;

namespace Blackwing.Shared.Content;

/// <summary>
/// Produces the key a tag is unique by. Two tags of the same kind owned by the same account are
/// the same tag exactly when their normalized values are equal.
/// </summary>
public static class TagNormalizer
{
    /// <summary>
    /// Trims, collapses inner whitespace, strips diacritics and upper-cases the value.
    /// </summary>
    /// <remarks>
    /// Accents are lost on purpose. Over months of typing, the realistic way to end up with two
    /// tags for one place is <c>Peñíscola</c> and <c>Peniscola</c>, so both must collide on the
    /// same key. The value the user actually typed, accents and capitalization included, is stored
    /// separately and is what the interface shows.
    /// </remarks>
    /// <exception cref="ArgumentException">The value is empty once normalized.</exception>
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var collapsed = CollapseWhitespace(value.Trim());
        var decomposed = collapsed.Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var normalized = builder.ToString().ToUpperInvariant();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A tag value cannot be empty.", nameof(value));
        }

        return normalized;
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            previousWasWhitespace = false;
            builder.Append(character);
        }

        return builder.ToString();
    }
}
