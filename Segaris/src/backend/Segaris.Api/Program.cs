using Segaris.Api.Composition;
using Segaris.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSegarisConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();
builder.Services.AddSegarisModules(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/health/live"));
app.MapHealthChecks("/health/live");
app.MapSegarisModules();

app.Run();

public partial class Program;
