using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Health.Domain;

/// <summary>Frozen Health defaults and validation bounds that are not catalogues.</summary>
internal static class HealthDefaults
{
    public const int NameMaximumLength = 200;
    public const int NotesMaximumLength = 2000;
    public const int SymptomsMaximumLength = 2000;
    public const int PosologyMaximumLength = 2000;
    public const int CategoryNameMaximumLength = 200;
    public const int MinimumAverageDurationDays = 1;
    public const int MaximumAverageDurationDays = 100_000;

    public const bool RequiresPrescription = false;
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;
}
