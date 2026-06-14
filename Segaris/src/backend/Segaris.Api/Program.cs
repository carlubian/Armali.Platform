using Scalar.AspNetCore;
using Segaris.Api.Composition;
using Segaris.Api.Configuration;
using Segaris.Api.Modules.Configuration.Seeding;
using Segaris.Api.Modules.Identity.Seeding;
using Segaris.Api.Persistence;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Observability;
using Serilog;

var databaseCommand = DatabaseCommand.Parse(args);
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = ApiRequestSizeMiddleware.DefaultMaximumRequestBodySize);
builder.Services.AddSegarisConfiguration(builder.Configuration, builder.Environment);
builder.AddSegarisLogging();
builder.Services.AddSegarisApiConventions();
builder.Services.AddHealthChecks();
builder.Services.AddSegarisObservability(builder.Configuration);
builder.Services.AddSegarisModules(builder.Configuration);
builder.Services.AddSegarisPersistence(builder.Configuration);

var app = builder.Build();

if (databaseCommand is not null)
{
    await app.Services.ExecuteDatabaseCommandAsync(databaseCommand, app.Environment);
    return;
}

await app.Services.MigrateSegarisDatabaseAsync();
await app.Services.SeedIdentityAsync();
await app.Services.SeedConfigurationAsync();

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseMiddleware<ApiRequestSizeMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect("/health/live"));
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = registration => registration.Tags.Contains("ready", StringComparer.Ordinal),
});
app.MapSegarisModules();

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
