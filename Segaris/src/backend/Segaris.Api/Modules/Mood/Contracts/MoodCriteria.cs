namespace Segaris.Api.Modules.Mood.Contracts;

/// <summary>Emotional intensity or mental energy of a mood entry.</summary>
internal enum MoodEnergy
{
    Low,
    Medium,
    High,
}

/// <summary>How habitually good or bad the emotion is for the user.</summary>
internal enum MoodAlignment
{
    Negative,
    Medium,
    Positive,
}

/// <summary>The objective or purpose of the emotion.</summary>
/// <remarks>The value is spelled <c>Offensive</c> in code, API contracts, and documentation.</remarks>
internal enum MoodDirection
{
    Harmony,
    Defensive,
    Offensive,
    Stability,
}

/// <summary>The origin or motive of the emotion.</summary>
internal enum MoodSource
{
    Internal,
    External,
}
