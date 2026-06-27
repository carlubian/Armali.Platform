using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Belfalas.Persistence;

/// <summary>
/// Builds a <see cref="BelfalasDbContext"/> for design-time tooling (e.g.
/// <c>dotnet ef migrations add</c>) without requiring the API host. Uses a local
/// SQLite file that is never read at runtime.
/// </summary>
public sealed class BelfalasDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BelfalasDbContext>
{
    public BelfalasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BelfalasDbContext>()
            .UseSqlite("Data Source=belfalas.design.db")
            .Options;

        return new BelfalasDbContext(options);
    }
}
