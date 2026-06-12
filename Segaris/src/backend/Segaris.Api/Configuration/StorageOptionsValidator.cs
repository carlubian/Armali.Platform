using Microsoft.Extensions.Options;

namespace Segaris.Api.Configuration;

internal sealed class StorageOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(options.DataProtectionKeysPath))
        {
            return ValidateOptionsResult.Fail(
                "Segaris:Storage:DataProtectionKeysPath is required in Production.");
        }

        if (environment.IsProduction() && string.IsNullOrWhiteSpace(options.AttachmentsPath))
        {
            return ValidateOptionsResult.Fail(
                "Segaris:Storage:AttachmentsPath is required in Production.");
        }

        if (environment.IsProduction() && string.IsNullOrWhiteSpace(options.BackupsPath))
        {
            return ValidateOptionsResult.Fail(
                "Segaris:Storage:BackupsPath is required in Production.");
        }

        return ValidateOptionsResult.Success;
    }
}
