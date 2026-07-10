using Blackwing.Api.Configuration;
using Blackwing.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<BlackwingOptions>().Bind(builder.Configuration.GetSection(BlackwingOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddBlackwingPersistence(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<BlackwingDbContext>("postgres", tags: ["ready"]);
var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/", () => Results.Redirect("/health/live"));
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new() { Predicate = registration => registration.Tags.Contains("ready", StringComparer.Ordinal) });
app.Run();
public partial class Program;
