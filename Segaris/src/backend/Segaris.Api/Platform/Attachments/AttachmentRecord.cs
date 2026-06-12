namespace Segaris.Api.Platform.Attachments;

internal sealed class AttachmentRecord
{
    public int Id { get; set; }

    public string Module { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string StorageFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long Size { get; set; }

    public int CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
