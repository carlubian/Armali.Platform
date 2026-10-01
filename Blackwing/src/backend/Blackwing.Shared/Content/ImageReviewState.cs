namespace Blackwing.Shared.Content;

/// <summary>
/// Whether the owner has looked at an image yet. The values are stored as <see cref="int"/>.
/// </summary>
/// <remarks>
/// <see cref="Pending"/> means <em>I have not looked at this yet</em>. It is deliberately a
/// different thing from <em>reviewed and intentionally left without tags</em>, which is
/// <see cref="Reviewed"/> with an empty tag set: the absence of tags can never stand in for the
/// review state.
/// </remarks>
public enum ImageReviewState
{
    Pending = 0,
    Reviewed = 1,
}
