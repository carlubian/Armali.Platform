namespace Segaris.Api.Platform.Api;

internal sealed record ApiRequestBodyLimit
{
    public ApiRequestBodyLimit(long maximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        MaximumBytes = maximumBytes;
    }

    public long MaximumBytes { get; }
}
