using Segaris.Api.Modules.Mood.Contracts;

namespace Segaris.Api.Modules.Mood.Domain;

internal static class MoodDerivedEmotionMatrix
{
    private static readonly IReadOnlyDictionary<MoodCriteriaCombination, string> Mappings =
        new Dictionary<MoodCriteriaCombination, string>
        {
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.Internal)] = "Happy",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.External)] = "Playful",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.Internal)] = "Empowered",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.External)] = "Determined",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Defensive, MoodSource.Internal)] = "Vibing",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Defensive, MoodSource.External)] = "Amazed",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Stability, MoodSource.Internal)] = "Confident",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodDirection.Stability, MoodSource.External)] = "Proud",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Harmony, MoodSource.Internal)] = "Healing",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Harmony, MoodSource.External)] = "Excited",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Offensive, MoodSource.Internal)] = "Disciplined",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Offensive, MoodSource.External)] = "Crusader",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.Internal)] = "Analytic",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.External)] = "Startled",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Stability, MoodSource.Internal)] = "Energetic",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodDirection.Stability, MoodSource.External)] = "Focused",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.Internal)] = "FOMO",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.External)] = "Disappointed",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Offensive, MoodSource.Internal)] = "Frustrated",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Offensive, MoodSource.External)] = "Angry",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Defensive, MoodSource.Internal)] = "Tense",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Defensive, MoodSource.External)] = "Scared",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.Internal)] = "Unstable",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.External)] = "Anxious",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.Internal)] = "Optimistic",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.External)] = "Inspired",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.Internal)] = "Daring",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.External)] = "Bold",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Defensive, MoodSource.Internal)] = "Daydreaming",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Defensive, MoodSource.External)] = "Relieved",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Stability, MoodSource.Internal)] = "Serene",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodDirection.Stability, MoodSource.External)] = "Grateful",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Harmony, MoodSource.Internal)] = "Thoughtful",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Harmony, MoodSource.External)] = "Cheeky",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Offensive, MoodSource.Internal)] = "Conflicted",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Offensive, MoodSource.External)] = "Betrayed",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.Internal)] = "Productive",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.External)] = "Curious",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Stability, MoodSource.Internal)] = "Absorbed",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodDirection.Stability, MoodSource.External)] = "Surprised",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.Internal)] = "Awkward",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.External)] = "Wary",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Offensive, MoodSource.Internal)] = "Ashamed",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Offensive, MoodSource.External)] = "Uncomfortable",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Defensive, MoodSource.Internal)] = "Insecure",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Defensive, MoodSource.External)] = "Confused",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.Internal)] = "Doubtful",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.External)] = "Worried",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.Internal)] = "Safe",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Harmony, MoodSource.External)] = "Caring",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.Internal)] = "Introspective",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Offensive, MoodSource.External)] = "Self-Assured",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Defensive, MoodSource.Internal)] = "Indoor",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Defensive, MoodSource.External)] = "Satisfied",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Stability, MoodSource.Internal)] = "Peaceful",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodDirection.Stability, MoodSource.External)] = "Connected",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Harmony, MoodSource.Internal)] = "Self-Care",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Harmony, MoodSource.External)] = "Nostalgic",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Offensive, MoodSource.Internal)] = "Indifferent",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Offensive, MoodSource.External)] = "Distrustful",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.Internal)] = "Withdrawn",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Defensive, MoodSource.External)] = "Protective",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Stability, MoodSource.Internal)] = "Lazy",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodDirection.Stability, MoodSource.External)] = "Relaxed",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.Internal)] = "Indecisive",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Harmony, MoodSource.External)] = "Lonely",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Offensive, MoodSource.Internal)] = "Bitter",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Offensive, MoodSource.External)] = "Apathetic",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Defensive, MoodSource.Internal)] = "Sad",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Defensive, MoodSource.External)] = "Depleted",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.Internal)] = "Tired",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodDirection.Stability, MoodSource.External)] = "Burnout",
        };

    public static IReadOnlyDictionary<MoodCriteriaCombination, string> All => Mappings;

    public static IReadOnlyList<string> EmotionCodes =>
        Mappings.Values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    public static string Resolve(
        MoodEnergy energy,
        MoodAlignment alignment,
        MoodDirection direction,
        MoodSource source)
    {
        MoodValidation.ValidateEnergy(energy);
        MoodValidation.ValidateAlignment(alignment);
        MoodValidation.ValidateDirection(direction);
        MoodValidation.ValidateSource(source);

        var key = Key(energy, alignment, direction, source);
        if (!Mappings.TryGetValue(key, out var emotion))
        {
            throw new MoodValidationException(
                "The mood criteria combination is not supported.",
                MoodValidationReason.Criteria);
        }

        return emotion;
    }

    private static MoodCriteriaCombination Key(
        MoodEnergy energy,
        MoodAlignment alignment,
        MoodDirection direction,
        MoodSource source) =>
        new(energy, alignment, direction, source);
}

internal readonly record struct MoodCriteriaCombination(
    MoodEnergy Energy,
    MoodAlignment Alignment,
    MoodDirection Direction,
    MoodSource Source);
