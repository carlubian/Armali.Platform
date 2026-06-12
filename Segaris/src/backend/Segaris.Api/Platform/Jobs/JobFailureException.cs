namespace Segaris.Api.Platform.Jobs;

/// <summary>
/// Thrown by a handler to fail its job with a stable, safe failure code. The message stays
/// in structured logs; only the code reaches the persisted record and public status.
/// </summary>
internal sealed class JobFailureException(string failureCode, string message)
    : Exception(message)
{
    public string FailureCode { get; } = failureCode;
}
