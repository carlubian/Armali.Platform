namespace Segaris.Api.Modules.Games.Domain;

/// <summary>
/// Fixed highlight-colour palette for a playthrough <c>Section</c>. The persisted
/// value is the colour token, not a raw CSS or hex value; the frontend maps each
/// token to design-system-aware presentation styles. These are domain values,
/// persisted as bounded strings using the member names and exchanged on the wire
/// using the same names.
/// </summary>
internal enum SectionColor
{
    Blue,
    Green,
    Amber,
    Red,
    Purple,
    Pink,
    Teal,
    Indigo,
    Slate,
    Orange,
}
