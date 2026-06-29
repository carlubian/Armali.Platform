using Belfalas.Domain;

namespace Belfalas.Persistence.Seeding;

/// <summary>
/// Authored reference content for the v1 tropical world template. The renderer loads
/// the matching base map and atlas from <c>/assets/worlds/tropical-v1</c>.
/// </summary>
public static class WorldTemplateSeed
{
    public const string DefaultTemplateId = "tropical-v1";

    private static readonly string[] DenizenTypes = ["islander", "parrot"];

    private static readonly Dictionary<string, string[]> VariantsByCategory = new()
    {
        ["dwelling"] =
        [
            "buildings/hut-coral",
            "buildings/hut-teal",
            "buildings/stilt-house",
            "buildings/market-stall",
        ],
        ["flora"] =
        [
            "flora/palm",
            "flora/hibiscus",
            "flora/banana-clump",
            "flora/mangrove",
        ],
        ["landmark"] =
        [
            "landmarks/tide-totem",
            "landmarks/shell-shrine",
            "landmarks/sun-obelisk",
        ],
        ["denizen:islander"] =
        [
            "denizens/islander-a",
            "denizens/islander-b",
        ],
        ["denizen:parrot"] =
        [
            "denizens/parrot-a",
        ],
    };

    private static readonly AuthoredDistrict[] AuthoredDistricts =
    [
        new(
            "Lagoon Market",
            0,
            [
                new(8, 3, "flora"), new(9, 3, "dwelling"), new(10, 3, "dwelling"), new(11, 3, "flora"),
                new(7, 4, "dwelling"), new(8, 4, "dwelling"), new(9, 4, "landmark"), new(10, 4, "dwelling"), new(11, 4, "flora"),
                new(7, 5, "flora"), new(8, 5, "dwelling"), new(9, 5, "dwelling"), new(10, 5, "dwelling"), new(11, 5, "flora"),
                new(8, 6, "flora"), new(9, 6, "dwelling"), new(10, 6, "landmark"), new(11, 6, "dwelling"),
                new(9, 7, "flora"), new(10, 7, "dwelling"), new(11, 7, "dwelling"), new(12, 7, "flora"),
                new(10, 8, "flora"), new(11, 8, "landmark"),
            ],
            [
                new(8, 4), new(10, 4), new(7, 5), new(12, 5), new(9, 6), new(11, 6), new(10, 7), new(12, 7),
            ]),
        new(
            "Canopy Ward",
            1,
            [
                new(4, 8, "flora"), new(5, 8, "dwelling"), new(6, 8, "dwelling"), new(7, 8, "flora"),
                new(3, 9, "dwelling"), new(4, 9, "flora"), new(5, 9, "dwelling"), new(6, 9, "landmark"), new(7, 9, "dwelling"),
                new(3, 10, "flora"), new(4, 10, "dwelling"), new(5, 10, "dwelling"), new(6, 10, "dwelling"), new(7, 10, "flora"),
                new(4, 11, "dwelling"), new(5, 11, "flora"), new(6, 11, "dwelling"), new(7, 11, "landmark"),
                new(5, 12, "flora"), new(6, 12, "dwelling"), new(7, 12, "dwelling"), new(8, 12, "flora"),
                new(6, 13, "landmark"), new(7, 13, "flora"),
            ],
            [
                new(4, 9), new(6, 9), new(3, 10), new(7, 10), new(5, 11), new(8, 11), new(6, 12), new(7, 13),
            ]),
        new(
            "Harbor Steps",
            2,
            [
                new(15, 8, "flora"), new(16, 8, "dwelling"), new(17, 8, "dwelling"), new(18, 8, "flora"),
                new(14, 9, "dwelling"), new(15, 9, "dwelling"), new(16, 9, "landmark"), new(17, 9, "dwelling"), new(18, 9, "flora"),
                new(14, 10, "flora"), new(15, 10, "dwelling"), new(16, 10, "dwelling"), new(17, 10, "dwelling"), new(18, 10, "landmark"),
                new(15, 11, "dwelling"), new(16, 11, "flora"), new(17, 11, "dwelling"), new(18, 11, "dwelling"),
                new(16, 12, "flora"), new(17, 12, "dwelling"), new(18, 12, "dwelling"), new(19, 12, "flora"),
                new(17, 13, "landmark"), new(18, 13, "flora"),
            ],
            [
                new(15, 9), new(17, 9), new(14, 10), new(19, 10), new(16, 11), new(18, 11), new(17, 12), new(18, 13),
            ]),
        new(
            "Sunspire Grove",
            3,
            [
                new(10, 13, "flora"), new(11, 13, "dwelling"), new(12, 13, "dwelling"), new(13, 13, "flora"),
                new(9, 14, "dwelling"), new(10, 14, "flora"), new(11, 14, "dwelling"), new(12, 14, "landmark"), new(13, 14, "dwelling"),
                new(9, 15, "flora"), new(10, 15, "dwelling"), new(11, 15, "dwelling"), new(12, 15, "dwelling"), new(13, 15, "flora"),
                new(10, 16, "dwelling"), new(11, 16, "flora"), new(12, 16, "dwelling"), new(13, 16, "landmark"),
                new(11, 17, "flora"), new(12, 17, "dwelling"), new(13, 17, "dwelling"), new(14, 17, "flora"),
                new(12, 18, "landmark"), new(13, 18, "flora"),
            ],
            [
                new(10, 14), new(12, 14), new(9, 15), new(13, 15), new(11, 16), new(14, 16), new(12, 17), new(13, 18),
            ]),
    ];

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
            MapWidth = 24,
            MapHeight = 20,
            OriginX = 1536,
            OriginY = 96,
            CameraMinX = 0,
            CameraMinY = 0,
            CameraMaxX = 3072,
            CameraMaxY = 1536,
            AssetBasePath = "/assets/worlds/tropical-v1",
            AtlasKey = "tropical-v1",
        };

        AddCategoryContracts(template);
        AddVariants(template);

        foreach (var authoredDistrict in AuthoredDistricts)
        {
            template.Districts.Add(CreateDistrict(template.Id, authoredDistrict));
        }

        return template;
    }

    private static void AddCategoryContracts(WorldTemplate template)
    {
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
            AnchorY = 0.96,
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
            AnchorY = 0.92,
            SortOffsetY = 28,
            SupportsDenizens = true,
        });
    }

    private static void AddVariants(WorldTemplate template)
    {
        foreach (var (category, spriteKeys) in VariantsByCategory)
        {
            foreach (var spriteKey in spriteKeys)
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
    }

    private static District CreateDistrict(string templateId, AuthoredDistrict authoredDistrict)
    {
        var district = new District
        {
            Id = Guid.NewGuid(),
            WorldTemplateId = templateId,
            Name = authoredDistrict.Name,
            Slot = authoredDistrict.Slot,
        };

        foreach (var point in authoredDistrict.Plots)
        {
            district.Plots.Add(new Plot
            {
                Id = Guid.NewGuid(),
                DistrictId = district.Id,
                Category = point.Category,
                PositionX = point.X,
                PositionY = point.Y,
            });
        }

        foreach (var socket in authoredDistrict.DenizenSockets)
        {
            district.DenizenSockets.Add(new DenizenSocket
            {
                Id = Guid.NewGuid(),
                DistrictId = district.Id,
                PositionX = socket.X,
                PositionY = socket.Y,
                AnchorX = 0.5,
                AnchorY = 0.94,
                SortOffsetY = 3,
                CompatibleDenizenTypes = string.Join(',', DenizenTypes),
            });
        }

        AddEvolutionStages(district);
        return district;
    }

    private static void AddEvolutionStages(District district)
    {
        var buildingOrders = new HashSet<int>
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9,
            11, 12, 13, 14, 15, 16, 17, 18,
            21, 22, 23, 24, 26, 27, 29,
        };
        var denizenOrders = new Dictionary<int, string>
        {
            [10] = "islander",
            [19] = "parrot",
            [25] = "islander",
            [31] = "islander",
            [36] = "parrot",
            [42] = "islander",
            [48] = "parrot",
        };

        for (var order = 1; order <= Leveling.MaxLevel; order++)
        {
            var stage = new EvolutionStage
            {
                Id = Guid.NewGuid(),
                DistrictId = district.Id,
                Order = order,
                Kind = EvolutionStageKind.Upgrade,
            };

            if (buildingOrders.Contains(order))
            {
                stage.Kind = EvolutionStageKind.Building;
            }
            else if (denizenOrders.TryGetValue(order, out var denizenType))
            {
                stage.Kind = EvolutionStageKind.Denizen;
                stage.DenizenType = denizenType;
            }

            district.EvolutionStages.Add(stage);
        }
    }

    private sealed record AuthoredDistrict(
        string Name,
        int Slot,
        IReadOnlyList<AuthoredPlot> Plots,
        IReadOnlyList<AuthoredSocket> DenizenSockets);

    private sealed record AuthoredPlot(int X, int Y, string Category);

    private sealed record AuthoredSocket(int X, int Y);
}
