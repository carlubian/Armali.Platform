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
            .ThenInclude(district => district.Plots)
            .Include(template => template.Districts)
            .ThenInclude(district => district.DenizenSockets)
            .Include(template => template.Districts)
            .ThenInclude(district => district.EvolutionStages)
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

            existingDistrict.Name = seededDistrict.Name;
            SyncDistrictPlots(existingDistrict, seededDistrict);
            SyncDistrictDenizenSockets(existingDistrict, seededDistrict);
            SyncDistrictEvolutionStages(existingDistrict, seededDistrict);
        }
    }

    private static void SyncDistrictPlots(District existingDistrict, District seededDistrict)
    {
        var existingPlots = existingDistrict.Plots
            .OrderBy(plot => plot.PositionY)
            .ThenBy(plot => plot.PositionX)
            .ToList();
        var seededPlots = seededDistrict.Plots
            .OrderBy(plot => plot.PositionY)
            .ThenBy(plot => plot.PositionX)
            .ToList();

        for (var index = 0; index < seededPlots.Count; index++)
        {
            var seededPlot = seededPlots[index];
            if (index < existingPlots.Count)
            {
                existingPlots[index].Category = seededPlot.Category;
                existingPlots[index].PositionX = seededPlot.PositionX;
                existingPlots[index].PositionY = seededPlot.PositionY;
                continue;
            }

            existingDistrict.Plots.Add(new Plot
            {
                Id = Guid.NewGuid(),
                DistrictId = existingDistrict.Id,
                Category = seededPlot.Category,
                PositionX = seededPlot.PositionX,
                PositionY = seededPlot.PositionY,
            });
        }
    }

    private static void SyncDistrictDenizenSockets(District existingDistrict, District seededDistrict)
    {
        var existingSockets = existingDistrict.DenizenSockets
            .OrderBy(socket => socket.PositionY)
            .ThenBy(socket => socket.PositionX)
            .ToList();
        var seededSockets = seededDistrict.DenizenSockets
            .OrderBy(socket => socket.PositionY)
            .ThenBy(socket => socket.PositionX)
            .ToList();

        for (var index = 0; index < seededSockets.Count; index++)
        {
            var seededSocket = seededSockets[index];
            if (index < existingSockets.Count)
            {
                existingSockets[index].PositionX = seededSocket.PositionX;
                existingSockets[index].PositionY = seededSocket.PositionY;
                existingSockets[index].AnchorX = seededSocket.AnchorX;
                existingSockets[index].AnchorY = seededSocket.AnchorY;
                existingSockets[index].SortOffsetY = seededSocket.SortOffsetY;
                existingSockets[index].CompatibleDenizenTypes = seededSocket.CompatibleDenizenTypes;
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

    private static void SyncDistrictEvolutionStages(District existingDistrict, District seededDistrict)
    {
        foreach (var seededStage in seededDistrict.EvolutionStages)
        {
            var existingStage = existingDistrict.EvolutionStages
                .FirstOrDefault(stage => stage.Order == seededStage.Order);
            if (existingStage is null)
            {
                existingDistrict.EvolutionStages.Add(new EvolutionStage
                {
                    Id = Guid.NewGuid(),
                    DistrictId = existingDistrict.Id,
                    Order = seededStage.Order,
                    Kind = seededStage.Kind,
                    DenizenType = seededStage.DenizenType,
                });
                continue;
            }

            existingStage.Kind = seededStage.Kind;
            existingStage.DenizenType = seededStage.DenizenType;
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
