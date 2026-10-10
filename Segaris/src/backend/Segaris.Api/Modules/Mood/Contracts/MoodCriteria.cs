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

/// <summary>What the emotion moves the user to do.</summary>
internal enum MoodIntent
{
    Stay,
    Defend,
    Attack,
    Rebuild,
    Explore,
}
