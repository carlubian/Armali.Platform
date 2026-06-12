using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Segaris.Api.Configuration;
using Segaris.Persistence;

namespace Segaris.Api.Persistence;

internal static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddSegarisPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<SqliteInMemoryConnection>();

        services.AddDbContext<SegarisDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var provider = DatabaseProviderParser.Parse(databaseOptions.Provider!);
            var connectionString = configuration.GetConnectionString("Segaris")!;

            if (provider == DatabaseProvider.Sqlite
                && SqliteInMemoryConnection.IsInMemory(connectionString))
            {
                var connection = serviceProvider
                    .GetRequiredService<SqliteInMemoryConnection>()
                    .GetOrOpen(connectionString);
                options.UseSqlite(
                    connection,
                    sqlite => sqlite.MigrationsAssembly(SegarisDbContextOptions.SqliteMigrationsAssembly));
                return;
            }

            SegarisDbContextOptions.Configure(options, provider, connectionString);
        });

        services.AddScoped<DevelopmentDatabaseSeeder>();
        return services;
    }
}
