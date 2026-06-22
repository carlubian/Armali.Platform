using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Destinations.Domain;

/// <summary>Frozen validation bounds and creation defaults for Destinations.</summary>
internal static class DestinationsDefaults
{
    public const int NameMaximumLength = 200;
    public const int CountryMaximumLength = 200;
    public const int EntryRequirementsMaximumLength = 2000;
    public const int NotesMaximumLength = 2000;

    public const int PlaceNameMaximumLength = 200;
    public const int PlaceReviewMaximumLength = 2000;
    public const int PlaceAddressMaximumLength = 200;
    public const int MinimumPlaceRating = 1;
    public const int MaximumPlaceRating = 5;

    public const int CategoryNameMaximumLength = 200;
    public const int PlaceCategoryNameMaximumLength = 200;

    public const bool DefaultIsSchengenArea = false;

    /// <summary>New destinations default to <see cref="RecordVisibility.Public"/>.</summary>
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}
