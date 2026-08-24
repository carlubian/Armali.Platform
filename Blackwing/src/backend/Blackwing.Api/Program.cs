using Blackwing.Api.Configuration;
using Blackwing.Api.Platform.Observability;
using Blackwing.Api.Platform.Persistence;
using Blackwing.Api.Platform.Storage;
using Blackwing.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBlackwingConfiguration(builder.Configuration);
builder.AddBlackwingLogging();
builder.Services.AddBlackwingStorage();
builder.Services.AddBlackwingPersistence(provider =>
    provider.GetRequiredService<IConfiguration>()
        .GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"])
    .AddCheck<ImageStorageHealthCheck>("images", tags: ["ready"]);

var app = builder.Build();

app.Services.EnsureBlackwingStorageDirectories();

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
});
app.UseRouting();

app.MapGet("/", () => Results.Redirect("/health/live"));

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
