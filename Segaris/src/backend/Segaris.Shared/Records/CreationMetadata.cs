using Segaris.Shared.Identity;

namespace Segaris.Shared.Records;

public readonly record struct CreationMetadata
{
    public CreationMetadata(DateTimeOffset createdAt, UserId? createdBy)
    {
        if (createdAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Creation timestamps must use UTC.", nameof(createdAt));
        }

        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public DateTimeOffset CreatedAt { get; }

    public UserId? CreatedBy { get; }
}
