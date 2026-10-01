namespace Blackwing.Shared.Images;

/// <summary>
/// An upload that has been written to the staging area and hashed, but not yet promoted to its
/// final place. <see cref="ContentHash"/> is the SHA-256 of the content as 64 lowercase
/// hexadecimal characters.
/// </summary>
public sealed record StagedImage(string StagingId, string ContentHash, long ByteSize);
