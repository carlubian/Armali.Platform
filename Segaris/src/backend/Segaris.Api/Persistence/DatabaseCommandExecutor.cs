using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Segaris.Api.Configuration;
using Segaris.Persistence;

namespace Segaris.Api.Persistence;

internal static class DatabaseCommandExecutor
{
    public static async Task ExecuteDatabaseCommandAsync(
        this IServiceProvider services,
        DatabaseCommand command,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Database commands are available only in Development.");
        }

        await using var scope = services.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;
        var database = scopedServices.GetRequiredService<SegarisDbContext>();
        var seeder = scopedServices.GetRequiredService<DevelopmentDatabaseSeeder>();

        if (command.Kind == DatabaseCommandKind.Reset)
        {
            if (!command.Confirmed)
            {
                throw new InvalidOperationException("Database reset requires --confirm.");
            }

            ValidateResetTarget(scopedServices, database);
            await database.Database.EnsureDeletedAsync(cancellationToken);
            await database.Database.MigrateAsync(cancellationToken);
        }

        if (command.Kind == DatabaseCommandKind.Seed || command.Seed)
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }

    private static void ValidateResetTarget(IServiceProvider services, SegarisDbContext database)
    {
        var options = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var provider = DatabaseProviderParser.Parse(options.Provider!);
        var connectionString = database.Database.GetConnectionString()!;

        if (provider == DatabaseProvider.Sqlite)
        {
            var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
            if (dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var absolutePath = Path.GetFullPath(dataSource);
            Console.WriteLine($"Resetting SQLite database: {absolutePath}");
            return;
        }

        var postgres = new NpgsqlConnectionStringBuilder(connectionString);
        var host = postgres.Host ?? string.Empty;
        var databaseName = postgres.Database ?? string.Empty;
        var localHost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
        var disposableDatabase = databaseName.Contains("segaris", StringComparison.OrdinalIgnoreCase)
            && (databaseName.Contains("dev", StringComparison.OrdinalIgnoreCase)
                || databaseName.Contains("test", StringComparison.OrdinalIgnoreCase));

        if (!localHost || !disposableDatabase)
        {
            throw new InvalidOperationException(
                "PostgreSQL reset accepts only a local database whose name contains Segaris and Dev or Test.");
        }

        Console.WriteLine($"Resetting PostgreSQL database: {postgres.Host}/{postgres.Database}");
    }
}
