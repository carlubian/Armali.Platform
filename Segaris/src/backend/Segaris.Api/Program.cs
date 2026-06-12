using Segaris.Api.Composition;
using Segaris.Api.Configuration;
using Segaris.Api.Persistence;

var databaseCommand = DatabaseCommand.Parse(args);
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSegarisConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();
builder.Services.AddSegarisModules(builder.Configuration);
builder.Services.AddSegarisPersistence(builder.Configuration);

var app = builder.Build();

if (databaseCommand is not null)
{
    await app.Services.ExecuteDatabaseCommandAsync(databaseCommand, app.Environment);
    return;
}

await app.Services.MigrateSegarisDatabaseAsync();

app.MapGet("/", () => Results.Redirect("/health/live"));
app.MapHealthChecks("/health/live");
app.MapSegarisModules();

await app.RunAsync();

public partial class Program;
