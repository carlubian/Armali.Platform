using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Processes.Domain;

/// <summary>Frozen Processes creation defaults and validation limits.</summary>
internal static class ProcessesDefaults
{
    public const int NameMaximumLength = 200;
    public const int NotesMaximumLength = 4000;
    public const int StepDescriptionMaximumLength = 500;
    public const int StepNotesMaximumLength = 1000;

    public static readonly RecordVisibility Visibility = RecordVisibility.Public;

    /// <summary>
    /// The accepted initial ordered <c>ProcessCategory</c> values, seeded once in Wave 1
    /// and never reimposed after administrative changes.
    /// </summary>
    public static readonly IReadOnlyList<string> InitialCategories =
    [
        "Administrative",
        "Legal",
        "Tax",
        "Health",
        "Education",
        "Vehicle",
        "Housing",
        "Other",
    ];
}
