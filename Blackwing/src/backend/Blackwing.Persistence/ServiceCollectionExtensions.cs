using Blackwing.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
        services.AddIdentityCore<BlackwingUser>(options =>
        {
            options.User.RequireUniqueEmail = false;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<BlackwingDbContext>();
        return services;
    }
}

public sealed class BlackwingDbContext(DbContextOptions<BlackwingDbContext> options)
    : IdentityDbContext<BlackwingUser, IdentityRole<Guid>, Guid>(options);
