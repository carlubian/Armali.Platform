using System.Globalization;

namespace Segaris.Api.Modules.Projects.Domain;

/// <summary>
/// Computes the on-demand unified identifier for a project or activity. The identifier
/// is never persisted: it is computed from the current parent program code, the current
/// parent axis code, the stable six-digit item number, and the current item name, so
/// renaming an ancestor or moving an item changes the displayed identifier while the
/// stored number is preserved.
/// </summary>
internal static class ProjectIdentifier
{
    /// <summary>Number of digits the item number is padded to with leading zeros.</summary>
    public const int NumberDigits = 6;

    /// <summary>
    /// Formats the unified identifier as <c>PPPPAAAA-123456 nnnn</c>, where <c>PPPP</c>
    /// is the parent program code, <c>AAAA</c> the parent axis code, <c>123456</c> the
    /// six-digit zero-padded number, and <c>nnnn</c> the item name.
    /// </summary>
    public static string Format(string programCode, string axisCode, int number, string name) =>
        string.Concat(
            programCode,
            axisCode,
            "-",
            number.ToString("D" + NumberDigits, CultureInfo.InvariantCulture),
            " ",
            name);
}
