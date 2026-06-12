namespace Segaris.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Segaris:Database";

    public string? Provider { get; init; }
}

