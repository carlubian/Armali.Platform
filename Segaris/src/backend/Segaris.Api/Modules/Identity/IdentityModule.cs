using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Segaris.Api.Composition;
using Segaris.Api.Modules.Identity.ApiKeys;
using Segaris.Api.Modules.Identity.Configuration;
using Segaris.Api.Modules.Identity.Endpoints;
using Segaris.Api.Modules.Identity.Persistence;
using Segaris.Api.Modules.Identity.Security;
using Segaris.Api.Modules.Identity.Seeding;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Identity;

internal sealed class IdentityModule : ISegarisModule
{
    public string Name => "Identity";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, IdentityModelContributor>();
        services.AddScoped<IdentitySeeder>();
        services.AddScoped<ApiKeyService>();

        services
            .AddOptions<IdentityBootstrapOptions>()
            .Bind(
                configuration.GetSection(IdentityBootstrapOptions.SectionName),
                binder => binder.ErrorOnUnknownConfiguration = true);

        services.AddIdentity<SegarisUser, SegarisRole>(options =>
        {
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
        .AddEntityFrameworkStores<SegarisDbContext>()
        .AddDefaultTokenProviders();

        // Re-validate the security stamp on every request so deactivation and
        // password recovery invalidate active sessions promptly.
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.Zero);

        AddApiKeyAuthentication(services);

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "segaris.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            // Plain HTTP on a trusted household network; HTTPS and Secure cookies
            // become mandatory before any remote exposure.
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
            options.SlidingExpiration = true;
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
            options.Cookie.Name = "segaris.antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(IdentityPolicies.Admin, policy =>
                policy.RequireRole(PlatformRole.Admin.ToString()));

        ConfigureDataProtection(services, configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSessionEndpoints();
        endpoints.MapProfileEndpoints();
        endpoints.MapApiKeyEndpoints();
        endpoints.MapAdminUserEndpoints();
    }

    /// <summary>
    /// Adds the API-key scheme alongside the cookie scheme without altering cookie
    /// behaviour: a request carrying no <c>Authorization</c> header reaches exactly
    /// the same handler it reached before.
    /// </summary>
    private static void AddApiKeyAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            // AddIdentity pins both to the cookie application scheme. Routing them
            // through the selector is what lets a key-bearing request reach the key
            // handler. DefaultSignInScheme is deliberately left untouched, so
            // SignInManager keeps issuing cookies as before.
            options.DefaultAuthenticateScheme = ApiKeyAuthenticationDefaults.SelectorScheme;
            options.DefaultChallengeScheme = ApiKeyAuthenticationDefaults.SelectorScheme;
        })
        .AddPolicyScheme(
            ApiKeyAuthenticationDefaults.SelectorScheme,
            displayName: null,
            options => options.ForwardDefaultSelector = context =>
                context.Request.Headers.ContainsKey(Microsoft.Net.Http.Headers.HeaderNames.Authorization)
                    ? ApiKeyAuthenticationDefaults.Scheme
                    : IdentityConstants.ApplicationScheme)
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationDefaults.Scheme,
            configureOptions: null);
    }

    private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Segaris");

        var keysPath = configuration["Segaris:Storage:DataProtectionKeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
    }
}
