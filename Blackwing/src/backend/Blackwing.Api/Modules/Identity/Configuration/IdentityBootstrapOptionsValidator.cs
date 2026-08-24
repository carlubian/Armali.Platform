using Microsoft.Extensions.Options;

namespace Blackwing.Api.Modules.Identity.Configuration;

/// <summary>
/// Rejects a half-configured bootstrap. Leaving the section empty is fine and means "no
/// bootstrap"; supplying only one of the two values almost always means an environment variable
/// was misspelled, and failing the host at start is far better than starting with no
/// administrator and discovering it at the login screen.
/// </summary>
internal sealed class IdentityBootstrapOptionsValidator : IValidateOptions<IdentityBootstrapOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentityBootstrapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsConfigured)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            return ValidateOptionsResult.Fail(
                $"{IdentityBootstrapOptions.SectionName}:UserName is required when a bootstrap password is configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail(
                $"{IdentityBootstrapOptions.SectionName}:Password is required when a bootstrap user name is configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
