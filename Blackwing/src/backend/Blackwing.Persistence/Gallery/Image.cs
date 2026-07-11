using Blackwing.Shared.Ownership;

namespace Blackwing.Persistence.Gallery;

/// <summary>
/// A single uploaded picture owned by exactly one user. Bytes live in the image
/// store addressed by <see cref="Sha256"/>; this record holds only the hash and
/// metadata so the storage layout can evolve without rewrites. An image is
/// <em>pending review</em> until <see cref="ReviewedAt"/> is set — explicit and
/// distinct from carrying zero tags.
/// </summary>
public sealed class Image : IOwnedEntity
{
    private Image()
    {
    }

    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public long Bytes { get; private set; }
    public DateTimeOffset? CapturedAt { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>
    /// The date the gallery orders by: EXIF capture date when known, upload time
    /// otherwise. A database-generated, stored column so a single index backs the
    /// default ordering and keyset pagination stays stable and index-only.
    /// </summary>
    public DateTimeOffset EffectiveCapturedAt { get; private set; }

    public static Image Create(ImageValues values, Guid ownerUserId, DateTimeOffset uploadedAt)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(uploadedAt, nameof(uploadedAt));
        if (values.CapturedAt is { } captured) EnsureUtc(captured, nameof(values.CapturedAt));
        if (ownerUserId == Guid.Empty) throw new ArgumentException("An owner is required.", nameof(ownerUserId));
        return new Image
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Sha256 = NormalizeSha256(values.Sha256),
            ContentType = Required(values.ContentType, nameof(values.ContentType)),
            Width = Positive(values.Width, nameof(values.Width)),
            Height = Positive(values.Height, nameof(values.Height)),
            Bytes = Positive(values.Bytes, nameof(values.Bytes)),
            CapturedAt = values.CapturedAt,
            UploadedAt = uploadedAt,
            ReviewedAt = null,
        };
    }

    /// <summary>Marks the image as reviewed. Idempotent: an already-reviewed image keeps its timestamp.</summary>
    public void MarkReviewed(DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        ReviewedAt ??= now;
    }

    private static string NormalizeSha256(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length != 64 || !normalized.All(Uri.IsHexDigit))
            throw new ArgumentException("A SHA-256 must be 64 hexadecimal characters.", nameof(value));
        return normalized;
    }

    private static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();

    private static int Positive(int value, string name) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, $"{name} must be positive.");

    private static long Positive(long value, string name) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, $"{name} must be positive.");

    private static void EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException($"{name} must be UTC.", name);
    }
}

/// <summary>Intrinsic, upload-time facts about an image; identity and review state are assigned by the aggregate.</summary>
public sealed record ImageValues(
    string Sha256,
    string ContentType,
    int Width,
    int Height,
    long Bytes,
    DateTimeOffset? CapturedAt);
