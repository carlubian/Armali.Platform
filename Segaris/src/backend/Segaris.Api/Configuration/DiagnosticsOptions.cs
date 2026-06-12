namespace Segaris.Api.Configuration;

internal sealed class DiagnosticsOptions
{
    public const string SectionName = "Segaris:Diagnostics";

    public int MaxBodyBytes { get; init; } = 16 * 1024;

    public int PermitLimit { get; init; } = 30;

    public int WindowSeconds { get; init; } = 60;
}
