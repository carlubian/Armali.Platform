using Segaris.Api.Platform.Api;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Identity;

internal static class IdentityProfilePolicy
{
    public const string DefaultLanguage = "en-GB";
    public const int MaximumDisplayNameLength = 200;

    private static readonly HashSet<string> AllowedLanguages =
        new(StringComparer.Ordinal) { DefaultLanguage };

    private static readonly HashSet<string> AllowedAvatarContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    public static ProfileValues Validate(string? displayName, string? language)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var normalizedDisplayName = displayName?.Trim() ?? string.Empty;
        if (normalizedDisplayName.Length == 0 || normalizedDisplayName.Length > MaximumDisplayNameLength)
        {
            errors["displayName"] = [$"Display name must contain between 1 and {MaximumDisplayNameLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(language) || !AllowedLanguages.Contains(language))
        {
            errors["language"] = ["Language must be 'en-GB'."];
        }

        if (errors.Count > 0)
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: errors);
        }

        return new(normalizedDisplayName, language!);
    }

    /// <summary>
    /// Applies the shared display-name rule (trimmed, 1 to
    /// <see cref="MaximumDisplayNameLength"/> characters) without binding it to a
    /// language, so administrative editing reuses the same policy as the
    /// self-service profile.
    /// </summary>
    public static bool TryNormalizeDisplayName(string? displayName, out string normalized)
    {
        normalized = displayName?.Trim() ?? string.Empty;
        return normalized.Length is > 0 and <= MaximumDisplayNameLength;
    }

    public static void ValidateAvatar(string contentType)
    {
        var normalized = contentType.Split(';', 2)[0].Trim();
        if (!AllowedAvatarContentTypes.Contains(normalized))
        {
            throw new ApiProblemException(
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.BadRequest,
                "One or more request values are invalid.",
                errors: new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["file"] = ["Avatar must be a JPEG, PNG, or WebP image."],
                });
        }
    }

    public static AttachmentOwner AvatarOwner(int userId) =>
        new("Identity", "UserAvatar", userId.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static string AvatarUrl(int userId) => $"/api/users/{userId}/avatar";

    internal sealed record ProfileValues(string DisplayName, string Language);
}
