using System.Text.RegularExpressions;

namespace Segaris.Api.Platform.Observability;

internal static partial class FrontendDiagnosticRedactor
{
    private const string RedactedValue = "[REDACTED]";

    public static string? Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var redacted = KeyValueSecretPattern().Replace(value, match =>
            $"{match.Groups[1].Value}{RedactedValue}");
        return AuthorizationPattern().Replace(redacted, $"$1{RedactedValue}");
    }

    [GeneratedRegex(
        @"(?i)\b((?:password|passwd|token|api[-_]?key|cookie|connection[-_]?string)\b\s*[:=]\s*)([^\s,;]+)",
        RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueSecretPattern();

    [GeneratedRegex(
        @"(?i)\b(authorization\s*:\s*)(bearer|basic)\s+[^\s,;]+",
        RegexOptions.CultureInvariant)]
    private static partial Regex AuthorizationPattern();
}
