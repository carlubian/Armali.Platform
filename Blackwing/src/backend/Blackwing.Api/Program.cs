using Blackwing.Api.Configuration;
using Blackwing.Api.Gallery;
using Blackwing.Api.Identity;
using Blackwing.Api.Ingestion;
using Blackwing.Api.Observability;
using Blackwing.Api.Persistence;
using Blackwing.Api.Storage;
using Blackwing.Persistence;
using Blackwing.Persistence.Identity;
using Blackwing.Shared.Ownership;
using Blackwing.Shared.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<BlackwingOptions>().Bind(builder.Configuration.GetSection(BlackwingOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddBlackwingPersistence(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserScope, CurrentUserScope>();
builder.Services.AddSingleton<IImageStore, LocalImageStore>();
builder.Services.AddSingleton<IUploadStagingArea, LocalUploadStagingArea>();
builder.Services.AddSingleton<ImageProcessingService>();
builder.Services.AddSingleton<UploadSignal>();
builder.Services.AddHostedService<UploadProcessingWorker>();
builder.Services.AddScoped<GalleryMutationService>();
builder.Services.AddScoped<GalleryReadService>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddMetrics();
builder.Services.AddSingleton<IngestionMetrics>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceId());
builder.Services.Configure<InitialAdminOptions>(builder.Configuration.GetSection(InitialAdminOptions.SectionName));
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
// AddIdentityCookies() wires the application cookie's OnValidatePrincipal to
// SecurityStampValidator, which resolves ISecurityStampValidator on every authenticated
// request. AddSignInManager registers that validator (and lives in the ASP.NET Core
// framework, so it is attached here rather than in the Persistence class library).
new IdentityBuilder(typeof(BlackwingUser), typeof(IdentityRole<Guid>), builder.Services).AddSignInManager();
// This is a JSON API, not a server-rendered site: return status codes instead of redirecting
// unauthenticated/forbidden callers to login/access-denied pages. Only the redirect handlers
// are overridden so the security-stamp OnValidatePrincipal wired above stays in place.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
// The login limiter guards against brute force in production. Integration tests drive many
// logins through a single in-process host inside one window, so the limit is lifted there.
var loginPermitLimit = builder.Environment.IsEnvironment("Testing") ? int.MaxValue : 5;
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("login", limiter => { limiter.PermitLimit = loginPermitLimit; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; }));
builder.Services.AddHealthChecks().AddDbContextCheck<BlackwingDbContext>("postgres", tags: ["ready"]);
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseBlackwingResponseContext();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method)
        && !HttpMethods.IsHead(context.Request.Method)
        && !HttpMethods.IsOptions(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/api")
        && !context.Request.Path.Equals("/api/auth/antiforgery"))
        await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context);
    await next(context);
});
await app.Services.MigrateBlackwingDatabaseAsync();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
app.MapGet("/", () => Results.Redirect("/health/live"));
app.MapIdentityEndpoints();
app.MapUploadEndpoints();
app.MapGalleryEndpoints();
app.MapOpsEndpoints();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new() { Predicate = registration => registration.Tags.Contains("ready", StringComparer.Ordinal) });
app.Run();
public partial class Program;
