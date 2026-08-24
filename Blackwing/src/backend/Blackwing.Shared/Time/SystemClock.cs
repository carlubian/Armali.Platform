namespace Blackwing.Shared.Time;

/// <summary>The production <see cref="IClock"/>, backed by the machine clock.</summary>
public sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();

    private SystemClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
