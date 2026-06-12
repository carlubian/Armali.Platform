using Microsoft.Extensions.Options;

namespace Segaris.Api.Configuration;

internal sealed class DatabaseOptionsValidator(IConfiguration configuration)
    : IValidateOptions<DatabaseOptions>
{
    private static readonly string[] SupportedProviders = ["Sqlite", "Postgres"];

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            return ValidateOptionsResult.Fail(
                $"{DatabaseOptions.SectionName}:Provider is required.");
        }

        if (!SupportedProviders.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"{DatabaseOptions.SectionName}:Provider must be Sqlite or Postgres.");
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Segaris")))
        {
            return ValidateOptionsResult.Fail("ConnectionStrings:Segaris is required.");
        }

        return ValidateOptionsResult.Success;
    }
}

