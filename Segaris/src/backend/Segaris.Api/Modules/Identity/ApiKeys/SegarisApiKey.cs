namespace Segaris.Api.Modules.Identity.ApiKeys;

/// <summary>
/// A user-bound API key for non-browser clients. The usable token is never
/// persisted: <see cref="KeyId"/> is the lookup index and <see cref="SecretHash"/>
/// verifies the presented secret.
/// </summary>
internal sealed class SegarisApiKey
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string SecretHash { get; set; } = string.Empty;

    /// <summary>
    /// The owner's security stamp when the key was issued. Deactivation, password
    /// changes, and administrative security changes rotate the user's stamp, which
    /// invalidates the key through the same mechanism that invalidates sessions.
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Null when the key never expires automatically. It remains individually revocable.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
