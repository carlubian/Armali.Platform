using Microsoft.Extensions.Options;

namespace Blackwing.Api.Configuration;

internal sealed class DatabaseOptionsValidator(IConfiguration configuration)
    : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.Equals(options.Provider, DatabaseOptions.SupportedProvider, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"{DatabaseOptions.SectionName}:Provider must be {DatabaseOptions.SupportedProvider}.");
        }

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)))
        {
            return ValidateOptionsResult.Fail(
                $"ConnectionStrings:{DatabaseOptions.ConnectionStringName} is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
