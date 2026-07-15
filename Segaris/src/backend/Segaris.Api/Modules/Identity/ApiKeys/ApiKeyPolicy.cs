using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Identity.ApiKeys;

internal static class ApiKeyPolicy
{
    public const int MaximumNameLength = 100;

    /// <summary>
    /// A household user manages keys by hand, so the ceiling exists to bound
    /// accidental growth rather than to ration a scarce resource.
    /// </summary>
    public const int MaximumActiveKeysPerUser = 20;

    /// <summary>
    /// Last use is recorded for attribution, not for accounting. An agent can issue
    /// many calls in a burst, so the stored value is refreshed at most this often to
    /// keep reads from becoming writes.
    /// </summary>
    public static readonly TimeSpan LastUsedPrecision = TimeSpan.FromMinutes(5);

    public static string ValidateName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > MaximumNameLength)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["name"] = [$"Name must contain between 1 and {MaximumNameLength} characters."],
                });
        }

        return normalized;
    }

    public static DateTimeOffset? ValidateExpiration(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        if (expiresAt is null)
        {
            return null;
        }

        if (expiresAt.Value <= now)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["expiresAt"] = ["Expiration must be in the future."],
                });
        }

        return expiresAt.Value.ToUniversalTime();
    }
}
