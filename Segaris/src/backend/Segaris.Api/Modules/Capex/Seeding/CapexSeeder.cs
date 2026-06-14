using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Capex.Seeding;

internal sealed class CapexSeeder(SegarisDbContext database, IClock clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var existing = await database.Set<CapexCategory>()
            .ToDictionaryAsync(category => category.Code, StringComparer.Ordinal, cancellationToken);
        var now = clock.UtcNow;

        foreach (var seed in CapexCategoryCatalog.Categories)
        {
            if (!existing.TryGetValue(seed.Code, out var category))
            {
                database.Add(new CapexCategory { Code = seed.Code, Name = seed.Name, CreatedAt = now, UpdatedAt = now });
            }
            else if (!string.Equals(category.Name, seed.Name, StringComparison.Ordinal))
            {
                category.Name = seed.Name;
                category.UpdatedAt = now;
            }
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
