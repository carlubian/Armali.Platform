using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Configuration.Seeding;

/// <summary>
/// Runs the one-time catalog initialization contract for any catalog, regardless of
/// the owning module. A catalog is seeded at most once: the very first time it is
/// seen with no marker and no rows it receives its initial values and is marked; an
/// existing populated catalog (an upgrade, or one an administrator already filled)
/// is marked without mutation; and a marked catalog is left untouched even if an
/// administrator later empties it.
/// </summary>
internal sealed class CatalogInitializer(SegarisDbContext database, IClock clock)
{
    public async Task EnsureInitializedAsync(
        string catalogKey,
        Func<CancellationToken, Task<bool>> hasAnyRowsAsync,
        Func<DateTimeOffset, CancellationToken, Task> seedAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hasAnyRowsAsync);
        ArgumentNullException.ThrowIfNull(seedAsync);

        var alreadyInitialized = await database.Set<SegarisCatalogInitialization>()
            .AsNoTracking()
            .AnyAsync(marker => marker.CatalogKey == catalogKey, cancellationToken);
        if (alreadyInitialized)
        {
            return;
        }

        var now = clock.UtcNow;
        if (!await hasAnyRowsAsync(cancellationToken))
        {
            await seedAsync(now, cancellationToken);
        }

        database.Add(new SegarisCatalogInitialization
        {
            CatalogKey = catalogKey,
            InitializedAt = now,
        });

        await database.SaveChangesAsync(cancellationToken);
    }
}
