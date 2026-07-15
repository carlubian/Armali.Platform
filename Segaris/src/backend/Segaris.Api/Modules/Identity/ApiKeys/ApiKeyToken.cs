using System.Security.Cryptography;
using System.Text;

namespace Segaris.Api.Modules.Identity.ApiKeys;

/// <summary>
/// Issues and parses <c>segaris_&lt;keyId&gt;_&lt;secret&gt;</c> tokens.
/// </summary>
/// <remarks>
/// <para>
/// Both segments are lowercase hexadecimal. The alphabet is deliberately narrower
/// than the entropy would require: base64url contains the underscore that separates
/// the segments, so a token could not be split back apart unambiguously.
/// </para>
/// <para>
/// The secret is 256 bits of cryptographic randomness, so the stored verifier is a
/// plain SHA-256 digest. A password hasher would defend against dictionary attacks
/// that do not apply to a uniformly random secret, at the cost of a key derivation
/// on every authenticated request.
/// </para>
/// </remarks>
internal static class ApiKeyToken
{
    public const string Prefix = "segaris";

    private const int KeyIdBytes = 12;
    private const int SecretBytes = 32;

    public const int KeyIdLength = KeyIdBytes * 2;

    public static IssuedToken Issue()
    {
        var keyId = RandomHex(KeyIdBytes);
        var secret = RandomHex(SecretBytes);
        return new IssuedToken(keyId, $"{Prefix}_{keyId}_{secret}", Hash(secret));
    }

    public static bool TryParse(string? token, out string keyId, out string secret)
    {
        keyId = string.Empty;
        secret = string.Empty;

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var parts = token.Split('_');
        if (parts.Length != 3
            || !string.Equals(parts[0], Prefix, StringComparison.Ordinal)
            || parts[1].Length != KeyIdLength
            || parts[2].Length != SecretBytes * 2
            || !IsHex(parts[1])
            || !IsHex(parts[2]))
        {
            return false;
        }

        keyId = parts[1];
        secret = parts[2];
        return true;
    }

    public static string Hash(string secret) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public static bool Verify(string secret, string expectedHash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(secret)),
            Encoding.UTF8.GetBytes(expectedHash));

    private static bool IsHex(string value) =>
        value.All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static string RandomHex(int byteCount) =>
        Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(byteCount));

    internal sealed record IssuedToken(string KeyId, string Token, string SecretHash);
}
