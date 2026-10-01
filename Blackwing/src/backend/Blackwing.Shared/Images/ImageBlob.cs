namespace Blackwing.Shared.Images;

/// <summary>
/// A readable stored image variant. Disposing it releases the underlying stream.
/// </summary>
public sealed record ImageBlob(Stream Content, long Length, string ContentType) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
