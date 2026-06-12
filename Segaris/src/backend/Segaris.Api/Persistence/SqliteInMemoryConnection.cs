using Microsoft.Data.Sqlite;

namespace Segaris.Api.Persistence;

/// <summary>
/// Holds a single open connection for an in-memory SQLite database so the schema and
/// seeded data survive for the host lifetime instead of vanishing when individual
/// <c>DbContext</c> connections close. In-memory SQLite is a test convenience only;
/// the file and PostgreSQL providers never use this path.
/// </summary>
internal sealed class SqliteInMemoryConnection : IDisposable
{
    private readonly Lock _gate = new();
    private SqliteConnection? _connection;

    public static bool IsInMemory(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        return builder.Mode == SqliteOpenMode.Memory
            || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase);
    }

    public SqliteConnection GetOrOpen(string connectionString)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        lock (_gate)
        {
            if (_connection is null)
            {
                var connection = new SqliteConnection(connectionString);
                connection.Open();
                _connection = connection;
            }
        }

        return _connection;
    }

    public void Dispose() => _connection?.Dispose();
}
