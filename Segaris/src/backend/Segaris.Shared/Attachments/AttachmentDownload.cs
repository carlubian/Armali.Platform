namespace Segaris.Shared.Attachments;

public sealed record AttachmentDownload(AttachmentDescriptor Descriptor, Stream Content)
    : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
