using Microsoft.EntityFrameworkCore;
using Segaris.Api.Platform.Api;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Identity.ApiKeys;

/// <summary>
/// Issues, lists, revokes, and verifies user-bound API keys.
/// </summary>
internal sealed class ApiKeyService(SegarisDbContext database, IClock clock)
{
    public async Task<IssuedApiKey> IssueAsync(
        SegarisUser user,
        string? name,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var validatedName = ApiKeyPolicy.ValidateName(name);
        var validatedExpiration = ApiKeyPolicy.ValidateExpiration(expiresAt, now);

        var activeKeys = await database.Set<SegarisApiKey>()
            .CountAsync(key => key.UserId == user.Id && key.RevokedAt == null, cancellationToken);
        if (activeKeys >= ApiKeyPolicy.MaximumActiveKeysPerUser)
        {
            throw new ApiProblemException(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.Conflict,
                "The request conflicts with the current state.",
                detail: $"A user may hold at most {ApiKeyPolicy.MaximumActiveKeysPerUser} active API keys. Revoke an existing key first.");
        }

        var issued = ApiKeyToken.Issue();
        var record = new SegarisApiKey
        {
            UserId = user.Id,
            Name = validatedName,
            KeyId = issued.KeyId,
            SecretHash = issued.SecretHash,
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            CreatedAt = now,
            ExpiresAt = validatedExpiration,
        };

        database.Set<SegarisApiKey>().Add(record);
        await database.SaveChangesAsync(cancellationToken);

        // The only moment the usable token exists outside the caller's request.
        return new IssuedApiKey(record, issued.Token);
    }

    /// <summary>
    /// Newest first. The ordering is by identifier rather than creation time because
    /// keys are issued in identifier order anyway, and SQLite cannot sort a
    /// <see cref="DateTimeOffset"/> column.
    /// </summary>
    public async Task<IReadOnlyList<SegarisApiKey>> ListAsync(int userId, CancellationToken cancellationToken) =>
        await database.Set<SegarisApiKey>()
            .AsNoTracking()
            .Where(key => key.UserId == userId)
            .OrderByDescending(key => key.Id)
            .ToArrayAsync(cancellationToken);

    public async Task<bool> RevokeAsync(int userId, int keyId, CancellationToken cancellationToken)
    {
        var record = await database.Set<SegarisApiKey>()
            .FirstOrDefaultAsync(key => key.Id == keyId && key.UserId == userId, cancellationToken);

        if (record is null || record.RevokedAt is not null)
        {
            return false;
        }

        record.RevokedAt = clock.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Resolves a presented token to its key record. Every rejection reason returns
    /// null so the caller cannot distinguish an unknown key from a revoked, expired,
    /// or stale one.
    /// </summary>
    public async Task<SegarisApiKey?> FindVerifiedAsync(string token, CancellationToken cancellationToken)
    {
        if (!ApiKeyToken.TryParse(token, out var keyId, out var secret))
        {
            return null;
        }

        var record = await database.Set<SegarisApiKey>()
            .FirstOrDefaultAsync(key => key.KeyId == keyId, cancellationToken);

        if (record is null || !ApiKeyToken.Verify(secret, record.SecretHash))
        {
            return null;
        }

        var now = clock.UtcNow;
        if (record.RevokedAt is not null || (record.ExpiresAt is not null && record.ExpiresAt <= now))
        {
            return null;
        }

        return record;
    }

    /// <summary>
    /// Records use at <see cref="ApiKeyPolicy.LastUsedPrecision"/> granularity, so a
    /// burst of agent calls does not turn every read into a write.
    /// </summary>
    public async Task TouchAsync(SegarisApiKey key, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (key.LastUsedAt is not null && now - key.LastUsedAt.Value < ApiKeyPolicy.LastUsedPrecision)
        {
            return;
        }

        key.LastUsedAt = now;
        await database.SaveChangesAsync(cancellationToken);
    }

    internal sealed record IssuedApiKey(SegarisApiKey Record, string Token);
}
