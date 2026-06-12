namespace Segaris.Api.Configuration;

internal sealed class ObservabilityOptions
{
    public const string SectionName = "Segaris:Observability";

    public SeqOptions Seq { get; init; } = new();
}

internal sealed class SeqOptions
{
    public bool Enabled { get; init; }

    public string? ServerUrl { get; init; }

    public string? ApiKey { get; init; }

    public string MinimumLevel { get; init; } = "Information";
}
