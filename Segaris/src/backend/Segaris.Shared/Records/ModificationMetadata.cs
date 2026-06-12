using Segaris.Shared.Identity;

namespace Segaris.Shared.Records;

public readonly record struct ModificationMetadata
{
    public ModificationMetadata(DateTimeOffset updatedAt, UserId? updatedBy)
    {
        if (updatedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Modification timestamps must use UTC.", nameof(updatedAt));
        }

        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public DateTimeOffset UpdatedAt { get; }

    public UserId? UpdatedBy { get; }
}
