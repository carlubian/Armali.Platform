namespace Blackwing.Api.Configuration;

internal sealed class DatabaseOptions
{
    public const string SectionName = "Blackwing:Database";

    /// <summary>Name of the connection string entry that holds the PostgreSQL DSN.</summary>
    public const string ConnectionStringName = "Blackwing";

    /// <summary>The only supported provider. PostgreSQL is not configurable.</summary>
    public const string SupportedProvider = "Postgres";

    public string Provider { get; init; } = SupportedProvider;
}
