namespace Blackwing.Api.Modules.Identity.Configuration;

/// <summary>
/// Optional first-administrator bootstrap credentials. When both values are present, an
/// idempotent startup seed creates the administrator if no matching account exists.
/// </summary>
/// <remarks>
/// No credential is ever committed to source control; these values come from untracked
/// configuration, user secrets, or environment variables, and are only needed on a first start.
/// </remarks>
internal sealed class IdentityBootstrapOptions
{
    public const string SectionName = "Blackwing:Identity:Bootstrap";

    public string? UserName { get; init; }

    public string? Password { get; init; }

    /// <summary>
    /// True when <em>either</em> value is present. Half a configuration counts as configured on
    /// purpose, so the validator can reject it at startup instead of the seeder silently
    /// skipping the bootstrap and leaving the deployment with no way in.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UserName) || !string.IsNullOrWhiteSpace(Password);
}
