using Segaris.Api.Modules.Mood.Contracts;

namespace Segaris.Api.Modules.Mood.Domain;

internal static class MoodDerivedEmotionMatrix
{
    private static readonly IReadOnlyDictionary<MoodCriteriaCombination, string> Mappings =
        new Dictionary<MoodCriteriaCombination, string>
        {
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodIntent.Stay)] = "Happy",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodIntent.Defend)] = "Vibing",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodIntent.Attack)] = "Empowered",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodIntent.Rebuild)] = "Festive",
            [Key(MoodEnergy.High, MoodAlignment.Positive, MoodIntent.Explore)] = "Playful",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodIntent.Stay)] = "Productive",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodIntent.Defend)] = "Avoidant",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodIntent.Attack)] = "Disciplined",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodIntent.Rebuild)] = "Entrenched",
            [Key(MoodEnergy.High, MoodAlignment.Medium, MoodIntent.Explore)] = "Curious",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodIntent.Stay)] = "Vengeful",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodIntent.Defend)] = "Scared",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodIntent.Attack)] = "Angry",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodIntent.Rebuild)] = "Shaken",
            [Key(MoodEnergy.High, MoodAlignment.Negative, MoodIntent.Explore)] = "Anxious",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodIntent.Stay)] = "Satisfied",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodIntent.Defend)] = "In the bubble",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodIntent.Attack)] = "Confident",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodIntent.Rebuild)] = "Relieved",
            [Key(MoodEnergy.Medium, MoodAlignment.Positive, MoodIntent.Explore)] = "Inspired",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodIntent.Stay)] = "Energetic",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodIntent.Defend)] = "Reluctant",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodIntent.Attack)] = "Focused",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodIntent.Rebuild)] = "Unstable",
            [Key(MoodEnergy.Medium, MoodAlignment.Medium, MoodIntent.Explore)] = "Surprised",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodIntent.Stay)] = "Vindicated",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodIntent.Defend)] = "Startled",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodIntent.Attack)] = "Disappointed",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodIntent.Rebuild)] = "Uncomfortable",
            [Key(MoodEnergy.Medium, MoodAlignment.Negative, MoodIntent.Explore)] = "Cautious",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodIntent.Stay)] = "Lazy",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodIntent.Defend)] = "Daydreaming",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodIntent.Attack)] = "Secure",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodIntent.Rebuild)] = "Peaceful",
            [Key(MoodEnergy.Low, MoodAlignment.Positive, MoodIntent.Explore)] = "Introspective",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodIntent.Stay)] = "Fine",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodIntent.Defend)] = "Withdrawn",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodIntent.Attack)] = "Distrustful",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodIntent.Rebuild)] = "Protective",
            [Key(MoodEnergy.Low, MoodAlignment.Medium, MoodIntent.Explore)] = "Indecisive",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodIntent.Stay)] = "Gloating",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodIntent.Defend)] = "Depleted",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodIntent.Attack)] = "Bitter",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodIntent.Rebuild)] = "Sad",
            [Key(MoodEnergy.Low, MoodAlignment.Negative, MoodIntent.Explore)] = "Tired",
        };

    public static IReadOnlyDictionary<MoodCriteriaCombination, string> All => Mappings;

    public static IReadOnlyList<string> EmotionCodes =>
        Mappings.Values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

    public static string Resolve(
        MoodEnergy energy,
        MoodAlignment alignment,
        MoodIntent intent)
    {
        MoodValidation.ValidateEnergy(energy);
        MoodValidation.ValidateAlignment(alignment);
        MoodValidation.ValidateIntent(intent);

        var key = Key(energy, alignment, intent);
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
        MoodIntent intent) =>
        new(energy, alignment, intent);
}

internal readonly record struct MoodCriteriaCombination(
    MoodEnergy Energy,
    MoodAlignment Alignment,
    MoodIntent Intent);
