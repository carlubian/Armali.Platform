using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blackwing.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlackwingPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Blackwing");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string 'Blackwing' is required.");
        services.AddDbContext<BlackwingDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}

/// <summary>Phase 1 database boundary. Domain mappings arrive in phase 3.</summary>
public sealed class BlackwingDbContext(DbContextOptions<BlackwingDbContext> options) : DbContext(options);
