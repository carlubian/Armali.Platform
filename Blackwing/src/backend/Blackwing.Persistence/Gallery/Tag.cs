using Blackwing.Shared.Ownership;

namespace Blackwing.Persistence.Gallery;

/// <summary>
/// A free-text label owned by one user, of one fixed <see cref="TagType"/>.
/// <see cref="NormalizedValue"/> is the case- and whitespace-insensitive key that
/// makes the label unique within its owner and type, so the same label is one
/// reusable tag rather than many duplicates.
/// </summary>
public sealed class Tag : IOwnedEntity
{
    public const int ValueMaxLength = 128;

    private Tag()
    {
    }

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public TagType Type { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string NormalizedValue { get; private set; } = string.Empty;

    public static Tag Create(Guid ownerUserId, TagType type, string value)
    {
        if (ownerUserId == Guid.Empty) throw new ArgumentException("An owner is required.", nameof(ownerUserId));
        var display = (value ?? string.Empty).Trim();
        if (display.Length == 0) throw new ArgumentException("A tag value is required.", nameof(value));
        if (display.Length > ValueMaxLength) throw new ArgumentException($"A tag value may not exceed {ValueMaxLength} characters.", nameof(value));
        return new Tag
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Type = type,
            Value = display,
            NormalizedValue = TagNormalization.Normalize(display),
        };
    }
}
