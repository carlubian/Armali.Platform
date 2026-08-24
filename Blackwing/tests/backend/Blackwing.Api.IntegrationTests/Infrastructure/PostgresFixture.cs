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
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
