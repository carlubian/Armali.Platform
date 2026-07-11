using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Blackwing.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model and emit
/// migrations without the running application's configuration. The connection
/// string is a placeholder: migrations are generated from the model, not from a
/// live database.
/// </summary>
public sealed class BlackwingDbContextFactory : IDesignTimeDbContextFactory<BlackwingDbContext>
{
    public BlackwingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BlackwingDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=blackwing_design;Username=blackwing;Password=design")
            .Options;
        return new BlackwingDbContext(options);
    }
}
