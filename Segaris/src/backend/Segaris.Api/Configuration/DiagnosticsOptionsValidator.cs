using Microsoft.Extensions.Options;

namespace Segaris.Api.Configuration;

internal sealed class DiagnosticsOptionsValidator : IValidateOptions<DiagnosticsOptions>
{
    public ValidateOptionsResult Validate(string? name, DiagnosticsOptions options)
    {
        if (options.MaxBodyBytes is < 4_096 or > 65_536)
        {
            return ValidateOptionsResult.Fail(
                $"{DiagnosticsOptions.SectionName}:MaxBodyBytes must be between 4096 and 65536.");
        }

        if (options.PermitLimit is < 1 or > 100)
        {
            return ValidateOptionsResult.Fail(
                $"{DiagnosticsOptions.SectionName}:PermitLimit must be between 1 and 100.");
        }

        if (options.WindowSeconds is < 10 or > 3_600)
        {
            return ValidateOptionsResult.Fail(
                $"{DiagnosticsOptions.SectionName}:WindowSeconds must be between 10 and 3600.");
        }

        return ValidateOptionsResult.Success;
    }
}
