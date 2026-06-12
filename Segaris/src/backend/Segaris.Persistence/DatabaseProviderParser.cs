namespace Segaris.Persistence;

public static class DatabaseProviderParser
{
    public static DatabaseProvider Parse(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "SQLITE" => DatabaseProvider.Sqlite,
            "POSTGRES" => DatabaseProvider.Postgres,
            _ => throw new ArgumentOutOfRangeException(
                nameof(value), value, "Database provider must be Sqlite or Postgres."),
        };
    }
}
