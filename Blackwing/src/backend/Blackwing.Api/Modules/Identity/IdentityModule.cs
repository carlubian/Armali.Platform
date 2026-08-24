using Blackwing.Api.Modules.Identity.Configuration;
using Blackwing.Api.Modules.Identity.Endpoints;
using Blackwing.Api.Modules.Identity.Persistence;
using Blackwing.Api.Modules.Identity.Seeding;
using Blackwing.Persistence;
using Blackwing.Shared.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Blackwing.Api.Modules.Identity;

/// <summary>
/// The identity module, expressed as two extension methods rather than as a registered module
/// type. Blackwing has exactly one module, so a composition mechanism would be ceremony; keeping
/// the module a folder with a clear seam is what makes it replaceable by an Armali SSO later.
/// </summary>
internal static class IdentityModule
{
    public static IServiceCollection AddBlackwingIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // The persistence project never sees an Identity type; it only sees this contributor.
        services.AddSingleton<IBlackwingModelContributor, IdentityModelContributor>();
        services.AddScoped<IdentitySeeder>();

        services
            .AddOptions<IdentityBootstrapOptions>()
            .Bind(
                configuration.GetSection(IdentityBootstrapOptions.SectionName),
                binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<IdentityBootstrapOptions>, IdentityBootstrapOptionsValidator>();

        services.AddIdentity<BlackwingUser, BlackwingRole>(options =>
        {
            // Length beats composition rules: a long passphrase is both stronger and easier to
            // remember than a short string with a mandatory symbol in it.
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 1;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<BlackwingDbContext>()
        .AddDefaultTokenProviders();

        // Re-validate the security stamp on every request, so deactivating an account or
        // resetting its password cuts its live sessions on the very next request instead of up
        // to thirty minutes later.
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.Zero);

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "blackwing.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;

            // Plain HTTP on a trusted household network. HTTPS and Secure cookies become
            // mandatory before any remote exposure.
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = true;

            // This is an API, not a site with login pages: an unauthenticated or forbidden call
            // must answer with a status the client can act on, never with a redirect to HTML.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "blackwing.antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(IdentityPolicies.Admin, policy =>
                policy.RequireRole(PlatformRole.Admin.ToString()));

        ConfigureDataProtection(services, configuration);

        return services;
    }

    public static void MapBlackwingIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSessionEndpoints();
        endpoints.MapAdminUserEndpoints();
    }

    /// <summary>
    /// Persists the Data Protection key ring to the configured directory. Without it the keys
    /// live in the container's ephemeral filesystem and every restart silently invalidates every
    /// session cookie in the house.
    /// </summary>
    private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Blackwing");

        var keysPath = configuration["Blackwing:Storage:DataProtectionKeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
    }
}
