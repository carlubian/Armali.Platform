using Segaris.Api.Modules.Mood.Contracts;

namespace Segaris.Api.Modules.Mood.Domain;

/// <summary>
/// Single source of the fixed criteria vocabularies. The order mirrors the enum
/// declaration order and is part of the frozen <c>mood/options</c> contract.
/// </summary>
internal static class MoodCriteriaCatalog
{
    public static readonly IReadOnlyList<string> Energies = Enum.GetNames<MoodEnergy>();

    public static readonly IReadOnlyList<string> Alignments = Enum.GetNames<MoodAlignment>();

    public static readonly IReadOnlyList<string> Directions = Enum.GetNames<MoodDirection>();

    public static readonly IReadOnlyList<string> Sources = Enum.GetNames<MoodSource>();

    /// <summary>
    /// Total derived-emotion combinations: 3 Energy x 3 Alignment x 4 Direction x
    /// 2 Source. The code-backed matrix arrives in Wave 1 and must cover exactly
    /// this count.
    /// </summary>
    public const int DerivedEmotionCombinationCount = 72;
}
