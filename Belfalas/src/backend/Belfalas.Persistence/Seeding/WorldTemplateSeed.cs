using Belfalas.Domain;

namespace Belfalas.Persistence.Seeding;

/// <summary>
/// Placeholder reference content for the v1 world template. The structure exercises
/// every template table (districts, plots, variants, evolution stages); the concrete
/// sprite keys and full 50-stage sequences are authored in the world/asset wave.
/// </summary>
public static class WorldTemplateSeed
{
    public const string DefaultTemplateId = "tropical-v1";

    private const int DistrictCount = 4;
    private const int GridSize = 3;

    private static readonly string[] Categories = ["dwelling", "flora", "landmark"];
    private static readonly string[] DenizenTypes = ["islander", "parrot"];

    private static readonly Dictionary<string, string[]> VariantsByCategory = new()
    {
        ["dwelling"] = ["buildings/hut-a", "buildings/hut-b", "buildings/hut-c"],
        ["flora"] = ["flora/palm-a", "flora/palm-b"],
        ["landmark"] = ["landmarks/totem-a", "landmarks/totem-b"],
    };

    /// <summary>Builds the default template object graph with freshly generated identifiers.</summary>
    public static WorldTemplate CreateDefault()
    {
        var template = new WorldTemplate
        {
            Id = DefaultTemplateId,
            Theme = "tropical",
            Name = "Tropical Isle",
            TileWidth = 128,
            TileHeight = 64,
            MapWidth = 16,
            MapHeight = 16,
            OriginX = 1024,
            OriginY = 128,
            CameraMinX = 0,
            CameraMinY = 0,
            CameraMaxX = 2048,
            CameraMaxY = 1280,
            AssetBasePath = "/assets/worlds/tropical-v1",
            AtlasKey = "tropical-v1",
        };

        template.CategoryContracts.Add(new CategoryContract
        {
            Id = Guid.NewGuid(),
            WorldTemplateId = template.Id,
            Category = "dwelling",
            FootprintWidth = 1,
            FootprintHeight = 1,
            AnchorX = 0.5,
            AnchorY = 0.88,
            SortOffsetY = 12,
            SupportsDenizens = true,
        });
        template.CategoryContracts.Add(new CategoryContract
        {
            Id = Guid.NewGuid(),
            WorldTemplateId = template.Id,
            Category = "flora",
            FootprintWidth = 1,
            FootprintHeight = 1,
            AnchorX = 0.5,
            AnchorY = 0.94,
            SortOffsetY = 4,
            SupportsDenizens = false,
        });
        template.CategoryContracts.Add(new CategoryContract
        {
            Id = Guid.NewGuid(),
            WorldTemplateId = template.Id,
            Category = "landmark",
            FootprintWidth = 2,
            FootprintHeight = 2,
            AnchorX = 0.5,
            AnchorY = 0.9,
            SortOffsetY = 24,
            SupportsDenizens = true,
        });

        foreach (var category in Categories)
        {
            foreach (var spriteKey in VariantsByCategory[category])
            {
                template.Variants.Add(new Variant
                {
                    Id = Guid.NewGuid(),
                    WorldTemplateId = template.Id,
                    Category = category,
                    SpriteKey = spriteKey,
                });
            }
        }

        for (var slot = 0; slot < DistrictCount; slot++)
        {
            template.Districts.Add(CreateDistrict(template.Id, slot));
        }

        return template;
    }

    private static District CreateDistrict(string templateId, int slot)
    {
        var district = new District
        {
            Id = Guid.NewGuid(),
            WorldTemplateId = templateId,
            Name = $"District {slot + 1}",
            Slot = slot,
        };

        var categoryIndex = 0;
        for (var y = 0; y < GridSize; y++)
        {
            for (var x = 0; x < GridSize; x++)
            {
                district.Plots.Add(new Plot
                {
                    Id = Guid.NewGuid(),
                    DistrictId = district.Id,
                    Category = Categories[categoryIndex++ % Categories.Length],
                    PositionX = x,
                    PositionY = y,
                });
            }
        }

        for (var y = 0; y < GridSize; y++)
        {
            for (var x = 0; x < GridSize; x++)
            {
                district.DenizenSockets.Add(new DenizenSocket
                {
                    Id = Guid.NewGuid(),
                    DistrictId = district.Id,
                    PositionX = x,
                    PositionY = y,
                    AnchorX = 0.5,
                    AnchorY = 0.92,
                    SortOffsetY = 2,
                    CompatibleDenizenTypes = string.Join(',', DenizenTypes),
                });
            }
        }

        var order = 1;
        foreach (var _ in district.Plots)
        {
            district.EvolutionStages.Add(new EvolutionStage
            {
                Id = Guid.NewGuid(),
                DistrictId = district.Id,
                Order = order++,
                Kind = EvolutionStageKind.Building,
            });
        }

        foreach (var denizenType in DenizenTypes)
        {
            district.EvolutionStages.Add(new EvolutionStage
            {
                Id = Guid.NewGuid(),
                DistrictId = district.Id,
                Order = order++,
                Kind = EvolutionStageKind.Denizen,
                DenizenType = denizenType,
            });
        }

        district.EvolutionStages.Add(new EvolutionStage
        {
            Id = Guid.NewGuid(),
            DistrictId = district.Id,
            Order = order,
            Kind = EvolutionStageKind.Upgrade,
        });

        return district;
    }
}
