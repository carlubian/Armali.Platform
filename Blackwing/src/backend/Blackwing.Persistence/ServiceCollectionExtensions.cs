using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Persistence;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BlackwingDbContext"/> against PostgreSQL, the only supported provider.
    /// </summary>
    /// <remarks>
    /// The connection string is supplied as a resolver rather than as a value, and it is therefore
    /// read when the context is first created instead of at registration time. That matters: a test
    /// host built with <c>WebApplicationFactory</c> layers its own configuration sources after
    /// <c>Program</c> has run, so any value read eagerly from the builder's configuration would be
    /// the one the test replaced. The resolver is also not inspected here — <c>Blackwing.Api</c>
    /// validates it at host start, so a missing value surfaces as an options validation failure.
    /// </remarks>
    /// <param name="services">The service collection to add the context to.</param>
    /// <param name="connectionStringResolver">Resolves the PostgreSQL connection string.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddBlackwingPersistence(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionStringResolver)
    {
        ArgumentNullException.ThrowIfNull(connectionStringResolver);

        services.AddDbContext<BlackwingDbContext>((provider, options) =>
            options.UseNpgsql(connectionStringResolver(provider)));

        return services;
    }
}
