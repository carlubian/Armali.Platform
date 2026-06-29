using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Belfalas.Domain;

namespace Belfalas.Persistence.Seeding;

/// <summary>
/// Startup helpers that bring the SQLite database up to the latest migration and seed
/// the reference content (world templates). Seeding is idempotent and carries no game
/// logic; runtime state is created when eras are authored in later waves.
/// </summary>
public static class BelfalasDatabaseExtensions
{
    /// <summary>Applies pending migrations and seeds reference data.</summary>
    public static async Task MigrateAndSeedBelfalasAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BelfalasDbContext>();

        EnsureDatabaseDirectoryExists(database.Database.GetConnectionString());
        await database.Database.MigrateAsync(cancellationToken);
        await SeedReferenceDataAsync(database, cancellationToken);
    }

    /// <summary>Seeds reference data (the default world template) when it is absent.</summary>
    public static async Task SeedReferenceDataAsync(
        BelfalasDbContext database,
        CancellationToken cancellationToken = default)
    {
        var seededDefault = WorldTemplateSeed.CreateDefault();
        var existingDefault = await database.WorldTemplates
            .Include(template => template.CategoryContracts)
            .Include(template => template.Variants)
            .Include(template => template.Districts)
            .ThenInclude(district => district.DenizenSockets)
            .AsSplitQuery()
            .FirstOrDefaultAsync(template => template.Id == WorldTemplateSeed.DefaultTemplateId, cancellationToken);
        if (existingDefault is null)
        {
            database.WorldTemplates.Add(seededDefault);
            await database.SaveChangesAsync(cancellationToken);
            return;
        }

        SyncDefaultTemplateContract(existingDefault, seededDefault);
        await database.SaveChangesAsync(cancellationToken);
    }

    private static void SyncDefaultTemplateContract(WorldTemplate existing, WorldTemplate seeded)
    {
        existing.Theme = seeded.Theme;
        existing.Name = seeded.Name;
        existing.TileWidth = seeded.TileWidth;
        existing.TileHeight = seeded.TileHeight;
        existing.MapWidth = seeded.MapWidth;
        existing.MapHeight = seeded.MapHeight;
        existing.OriginX = seeded.OriginX;
        existing.OriginY = seeded.OriginY;
        existing.CameraMinX = seeded.CameraMinX;
        existing.CameraMinY = seeded.CameraMinY;
        existing.CameraMaxX = seeded.CameraMaxX;
        existing.CameraMaxY = seeded.CameraMaxY;
        existing.AssetBasePath = seeded.AssetBasePath;
        existing.AtlasKey = seeded.AtlasKey;

        foreach (var seededContract in seeded.CategoryContracts)
        {
            var existingContract = existing.CategoryContracts
                .FirstOrDefault(contract => contract.Category == seededContract.Category);
            if (existingContract is null)
            {
                seededContract.WorldTemplate = null;
                existing.CategoryContracts.Add(seededContract);
                continue;
            }

            existingContract.FootprintWidth = seededContract.FootprintWidth;
            existingContract.FootprintHeight = seededContract.FootprintHeight;
            existingContract.AnchorX = seededContract.AnchorX;
            existingContract.AnchorY = seededContract.AnchorY;
            existingContract.SortOffsetY = seededContract.SortOffsetY;
            existingContract.SupportsDenizens = seededContract.SupportsDenizens;
        }

        foreach (var seededVariant in seeded.Variants)
        {
            var exists = existing.Variants.Any(variant =>
                variant.Category == seededVariant.Category && variant.SpriteKey == seededVariant.SpriteKey);
            if (exists)
            {
                continue;
            }

            seededVariant.WorldTemplate = null;
            existing.Variants.Add(seededVariant);
        }

        foreach (var existingDistrict in existing.Districts)
        {
            var seededDistrict = seeded.Districts.FirstOrDefault(district => district.Slot == existingDistrict.Slot);
            if (seededDistrict is null)
            {
                continue;
            }

            foreach (var seededSocket in seededDistrict.DenizenSockets)
            {
                var exists = existingDistrict.DenizenSockets.Any(socket =>
                    socket.PositionX == seededSocket.PositionX && socket.PositionY == seededSocket.PositionY);
                if (exists)
                {
                    continue;
                }

                existingDistrict.DenizenSockets.Add(new DenizenSocket
                {
                    Id = Guid.NewGuid(),
                    DistrictId = existingDistrict.Id,
                    PositionX = seededSocket.PositionX,
                    PositionY = seededSocket.PositionY,
                    AnchorX = seededSocket.AnchorX,
                    AnchorY = seededSocket.AnchorY,
                    SortOffsetY = seededSocket.SortOffsetY,
                    CompatibleDenizenTypes = seededSocket.CompatibleDenizenTypes,
                });
            }
        }
    }

    /// <summary>Creates the directory holding the SQLite file when it does not yet exist.</summary>
    private static void EnsureDatabaseDirectoryExists(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
