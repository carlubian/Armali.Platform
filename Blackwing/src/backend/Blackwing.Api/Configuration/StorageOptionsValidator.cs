using Microsoft.Extensions.Options;

namespace Blackwing.Api.Configuration;

internal sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ImagesPath))
        {
            return ValidateOptionsResult.Fail($"{StorageOptions.SectionName}:ImagesPath is required.");
        }

        if (!IsUsablePath(options.ImagesPath))
        {
            return ValidateOptionsResult.Fail(
                $"{StorageOptions.SectionName}:ImagesPath must be a valid file system path.");
        }

        if (!string.IsNullOrWhiteSpace(options.DataProtectionKeysPath)
            && !IsUsablePath(options.DataProtectionKeysPath))
        {
            return ValidateOptionsResult.Fail(
                $"{StorageOptions.SectionName}:DataProtectionKeysPath must be a valid file system path.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsUsablePath(string path)
    {
        try
        {
            _ = Path.GetFullPath(path);
            return true;
        }
        catch (Exception exception)
            when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
