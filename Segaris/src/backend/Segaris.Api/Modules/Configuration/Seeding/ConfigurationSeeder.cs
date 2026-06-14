using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Configuration.Seeding;

internal sealed class ConfigurationSeeder(SegarisDbContext database, IClock clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        await UpsertAsync(database.Set<SegarisSupplier>(), ConfigurationCatalog.Suppliers, now, cancellationToken);
        await UpsertAsync(database.Set<SegarisCostCenter>(), ConfigurationCatalog.CostCenters, now, cancellationToken);
        await UpsertAsync(database.Set<SegarisCurrency>(), ConfigurationCatalog.Currencies, now, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertAsync<TEntity>(
        DbSet<TEntity> entities,
        IReadOnlyList<CatalogSeed> seeds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
        where TEntity : class, IConfigurationCatalogEntity, new()
    {
        var existing = await entities.ToDictionaryAsync(
            entity => entity.Code,
            StringComparer.Ordinal,
            cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existing.TryGetValue(seed.Code, out var entity))
            {
                entity = new TEntity();
                entities.Add(entity);
                entity.Code = seed.Code;
                entity.Name = seed.Name;
                entity.CreatedAt = now;
                entity.UpdatedAt = now;
                continue;
            }

            if (!string.Equals(
                entity.Name,
                seed.Name,
                StringComparison.Ordinal))
            {
                entity.Name = seed.Name;
                entity.UpdatedAt = now;
            }
        }
    }
}
