using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Maintenance.Domain;

internal static class MaintenanceDefaults
{
    public const int TitleMaximumLength = 200;
    public const int NotesMaximumLength = 4000;

    public static readonly MaintenanceStatus Status = MaintenanceStatus.Pending;
    public static readonly MaintenancePriority Priority = MaintenancePriority.Medium;
    public static readonly RecordVisibility Visibility = RecordVisibility.Public;

    /// <summary>
    /// The accepted initial ordered <c>MaintenanceType</c> values, seeded once in
    /// Wave 1 and never reimposed after administrative changes.
    /// </summary>
    public static readonly IReadOnlyList<string> InitialTypes =
    [
        "Repair",
        "Preventive",
        "Inspection",
        "Cleaning",
        "Installation",
        "Other",
    ];
}
