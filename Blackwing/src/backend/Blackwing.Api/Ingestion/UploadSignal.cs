namespace Blackwing.Api.Ingestion;

/// <summary>
/// A lightweight wake-up between the upload endpoint and the worker. Staging a new
/// job pulses the signal so the worker starts immediately instead of waiting for its
/// next idle poll. Multiple pulses before a wait collapse into one — the worker
/// always drains every pending job once woken, so no notification is ever lost.
/// </summary>
public sealed class UploadSignal
{
    private readonly SemaphoreSlim gate = new(0, 1);

    /// <summary>Wakes the worker if it is idle; a no-op if a wake is already pending.</summary>
    public void Notify()
    {
        try { gate.Release(); }
        catch (SemaphoreFullException) { /* A wake is already pending; nothing to add. */ }
    }

    /// <summary>Waits for a wake-up or until <paramref name="timeout"/> elapses (the idle poll fallback).</summary>
    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => gate.WaitAsync(timeout, cancellationToken);
}
