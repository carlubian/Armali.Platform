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
        services.AddDbContext<SegarisDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var provider = DatabaseProviderParser.Parse(databaseOptions.Provider!);
            var connectionString = configuration.GetConnectionString("Segaris")!;

            SegarisDbContextOptions.Configure(options, provider, connectionString);
        });

        services.AddScoped<DevelopmentDatabaseSeeder>();
        return services;
    }
}
