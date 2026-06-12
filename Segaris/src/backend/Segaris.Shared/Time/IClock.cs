namespace Segaris.Shared.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
