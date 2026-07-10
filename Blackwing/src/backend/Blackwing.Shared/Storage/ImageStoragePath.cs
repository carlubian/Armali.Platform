namespace Blackwing.Shared.Storage;

/// <summary>
/// Pure resolver for the content-addressable, per-user on-disk layout:
/// <c>{root}/{userId}/{ab}/{cd}/{sha256}.orig|.preview.webp|.thumb.webp</c>.
/// The first two byte-pairs of the hash shard the tree so no directory holds an
/// unbounded number of files. The layout is derived entirely from the owner and
/// the SHA-256, never stored, so it can evolve without rewriting records.
/// </summary>
public static class ImageStoragePath
{
    public static string Extension(ImageDerivative derivative) => derivative switch
    {
        ImageDerivative.Original => ".orig",
        ImageDerivative.Preview => ".preview.webp",
        ImageDerivative.Thumbnail => ".thumb.webp",
        _ => throw new ArgumentOutOfRangeException(nameof(derivative), derivative, "Unknown image derivative."),
    };

    /// <summary>The owner directory relative to the storage root (<c>{userId}</c>).</summary>
    public static string OwnerSegment(Guid ownerUserId) => ownerUserId.ToString("N");

    /// <summary>The sharded directory of a hash relative to the owner (<c>{ab}/{cd}</c>).</summary>
    public static string ShardSegment(string sha256)
    {
        var normalized = Normalize(sha256);
        return Path.Combine(normalized[..2], normalized[2..4]);
    }

    /// <summary>The full path of one derivative under the given storage root.</summary>
    public static string Resolve(string root, Guid ownerUserId, string sha256, ImageDerivative derivative)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("A storage root is required.", nameof(root));
        var normalized = Normalize(sha256);
        return Path.Combine(root, OwnerSegment(ownerUserId), normalized[..2], normalized[2..4], normalized + Extension(derivative));
    }

    private static string Normalize(string sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256)) throw new ArgumentException("A SHA-256 is required.", nameof(sha256));
        var value = sha256.Trim().ToLowerInvariant();
        if (value.Length != 64 || !value.All(Uri.IsHexDigit))
            throw new ArgumentException("A SHA-256 must be 64 hexadecimal characters.", nameof(sha256));
        return value;
    }
}
