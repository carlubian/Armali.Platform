using Blackwing.Api.Configuration;
using Blackwing.Api.Modules.Identity;
using Blackwing.Api.Modules.Identity.Seeding;
using Blackwing.Api.Platform.Api;
using Blackwing.Api.Platform.Observability;
using Blackwing.Api.Platform.Ownership;
using Blackwing.Api.Platform.Persistence;
using Blackwing.Api.Platform.Storage;
using Blackwing.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBlackwingConfiguration(builder.Configuration);
builder.AddBlackwingLogging();
builder.Services.AddBlackwingApiConventions();
builder.Services.AddBlackwingStorage();
builder.Services.AddBlackwingPersistence(provider =>
    provider.GetRequiredService<IConfiguration>()
        .GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty);
builder.Services.AddBlackwingIdentity(builder.Configuration);
builder.Services.AddBlackwingRateLimiting();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"])
    .AddCheck<ImageStorageHealthCheck>("images", tags: ["ready"]);

var app = builder.Build();

app.Services.EnsureBlackwingStorageDirectories();

// Migrations are applied at startup and the identity seed runs straight after, so a deployment
// against an empty database is operable without a manual step. Both are idempotent.
await app.Services.MigrateBlackwingDatabaseAsync();
await app.Services.SeedIdentityAsync();

// First in the pipeline, before anything reads the client address. Behind the Caddy ingress the
// connection address is always Caddy's, and the login rate limiter partitions by client IP.
app.UseForwardedHeaders();

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect("/health/live"));

// Both health endpoints stay anonymous and aggregated. Caddy publishes them on port 5526, so
// they must never report check names, exception messages, or connection strings.
//
// Liveness answers for the process alone. The predicate excludes every registered check on
// purpose: mapping the endpoint without one would evaluate all of them, and a database outage
// would then report the process as dead and get the container restarted for no reason.
app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new()
{
    Predicate = registration => registration.Tags.Contains("ready", StringComparer.Ordinal),
});

app.MapBlackwingIdentityEndpoints();
app.MapOwnershipProbeEndpoints();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

await app.RunAsync();

public partial class Program;
