using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Composition;
using Segaris.Persistence;

namespace Segaris.Api.Persistence;

public sealed class SegarisDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SegarisDbContext>
{
    public SegarisDbContext CreateDbContext(string[] args)
    {
        var providerName = GetArgument(args, "--provider") ?? "Sqlite";
        var provider = DatabaseProviderParser.Parse(providerName);
        var connectionString = GetArgument(args, "--connection")
            ?? (provider == DatabaseProvider.Sqlite
                ? "Data Source=segaris.design.db"
                : "Host=localhost;Database=segaris_design;Username=postgres;Password=postgres");

        var services = new ServiceCollection();
        services.AddSegarisModules(new ConfigurationBuilder().Build());
        using var serviceProvider = services.BuildServiceProvider();
        var contributors = serviceProvider.GetServices<ISegarisModelContributor>().ToArray();
        var options = new DbContextOptionsBuilder<SegarisDbContext>();
        SegarisDbContextOptions.Configure(options, provider, connectionString);

        return new SegarisDbContext(options.Options, contributors);
    }

    private static string? GetArgument(string[] args, string name)
    {
        var index = Array.FindIndex(args, value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
