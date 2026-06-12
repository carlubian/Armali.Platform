using Microsoft.Extensions.Options;
using Serilog.Events;

namespace Segaris.Api.Configuration;

internal sealed class ObservabilityOptionsValidator : IValidateOptions<ObservabilityOptions>
{
    public ValidateOptionsResult Validate(string? name, ObservabilityOptions options)
    {
        if (!Enum.TryParse<LogEventLevel>(options.Seq.MinimumLevel, ignoreCase: true, out _))
        {
            return ValidateOptionsResult.Fail(
                $"{ObservabilityOptions.SectionName}:Seq:MinimumLevel must be a valid Serilog level.");
        }

        if (!options.Seq.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.Seq.ServerUrl)
            || !Uri.TryCreate(options.Seq.ServerUrl, UriKind.Absolute, out var serverUri)
            || (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail(
                $"{ObservabilityOptions.SectionName}:Seq:ServerUrl must be an absolute HTTP or HTTPS URL when Seq is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
