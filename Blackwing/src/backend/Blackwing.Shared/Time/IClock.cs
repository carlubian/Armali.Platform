namespace Blackwing.Shared.Time;

/// <summary>
/// Abstracts the wall clock so anything that stamps a timestamp can be tested
/// deterministically. Every value it yields is UTC.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
