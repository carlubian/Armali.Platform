using Npgsql;
using Testcontainers.PostgreSql;

namespace Blackwing.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Starts one real PostgreSQL container for the whole integration collection and exposes its
/// connection string. Integration tests run against the same engine as production; nothing is
/// faked here.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("blackwing_test")
        .WithUsername("blackwing")
        .WithPassword("blackwing")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Creates an empty database inside the running container and returns a connection string
    /// pointing at it.
    /// </summary>
    /// <remarks>
    /// Every test host that migrates and seeds needs a database of its own, or the bootstrap
    /// administrator, the accounts and the ownership probes of one test would be visible to the
    /// next. Paying for that isolation with one container per class would multiply the runtime of
    /// the suite by the number of classes; a <c>CREATE DATABASE</c> inside the container that is
    /// already running costs milliseconds instead.
    /// </remarks>
    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        // The name is generated from a GUID and never comes from test input, so it cannot carry
        // an identifier needing more quoting than the quotes already written here.
        var name = $"blackwing_{Guid.NewGuid():N}";

        var result = await _container.ExecScriptAsync($"CREATE DATABASE \"{name}\";", cancellationToken);

        // psql exits zero even when a statement fails, so the error stream is the real signal.
        if (result.ExitCode != 0 || !string.IsNullOrWhiteSpace(result.Stderr))
        {
            throw new InvalidOperationException(
                $"Could not create the test database '{name}': {result.Stderr}");
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = name,
        }.ConnectionString;
    }

    /// <summary>
    /// Drops a database created by <see cref="CreateDatabaseAsync"/>, terminating whatever
    /// sessions are still open against it. This is how a test takes PostgreSQL away from a host
    /// that is already running.
    /// </summary>
    public async Task DropDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The connection string names no database.", nameof(connectionString));
        }

        // WITH (FORCE) terminates the connections the test host still holds in its pool; without
        // it PostgreSQL refuses to drop a database that is in use.
        var result = await _container.ExecScriptAsync($"DROP DATABASE \"{name}\" WITH (FORCE);", cancellationToken);
        if (result.ExitCode != 0 || !string.IsNullOrWhiteSpace(result.Stderr))
        {
            throw new InvalidOperationException(
                $"Could not drop the test database '{name}': {result.Stderr}");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
