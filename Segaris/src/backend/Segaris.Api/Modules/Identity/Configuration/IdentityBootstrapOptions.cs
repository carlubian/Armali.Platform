namespace Segaris.Api.Modules.Identity.Configuration;

/// <summary>
/// Optional first-administrator bootstrap credentials. When both values are present,
/// an idempotent startup seed creates the administrator if no matching account exists.
/// No credential is ever committed to source control; these values come from
/// untracked configuration, user secrets, or environment variables.
/// </summary>
public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "Segaris:Identity:Bootstrap";

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UserName) || !string.IsNullOrWhiteSpace(Password);
}
