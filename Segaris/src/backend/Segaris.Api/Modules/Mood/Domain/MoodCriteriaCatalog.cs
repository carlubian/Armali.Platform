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

    public static readonly IReadOnlyList<string> Intents = Enum.GetNames<MoodIntent>();

    public static IReadOnlyList<string> Emotions => MoodDerivedEmotionMatrix.EmotionCodes;

    /// <summary>
    /// Total derived-emotion combinations: 3 Energy x 3 Alignment x 5 Intent.
    /// The code-backed matrix must cover exactly this count.
    /// </summary>
    public const int DerivedEmotionCombinationCount = 45;
}
