using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Belfalas.Api.Contracts;

namespace Belfalas.Tests;

public sealed class WorldApiTests
{
    private const string TemplateId = "tropical-v1";

    [Fact]
    public async Task World_template_endpoint_returns_the_seeded_template_model()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var templates = await client.GetFromJsonAsync<List<WorldTemplateResponse>>("/api/world/templates");

        Assert.NotNull(templates);
        var template = Assert.Single(templates, candidate => candidate.Id == TemplateId);
        Assert.Equal(TemplateId, template.Id);
        Assert.Equal(128, template.Render.TileWidth);
        Assert.Equal(64, template.Render.TileHeight);
        Assert.Equal(24, template.Render.MapWidth);
        Assert.Equal(20, template.Render.MapHeight);
        Assert.Equal("/assets/worlds/tropical-v1", template.Render.AssetBasePath);
        Assert.Contains(template.Categories, category =>
            category.Category == "dwelling" &&
            category.FootprintWidth == 1 &&
            category.SupportsDenizens);
        Assert.Equal(4, template.Districts.Count);
        Assert.Equal(
            ["Lagoon Market", "Canopy Ward", "Harbor Steps", "Sunspire Grove"],
            template.Districts.Select(district => district.Name).ToArray());
        Assert.All(template.Districts, district =>
        {
            Assert.Equal(24, district.Plots.Count);
            Assert.Equal(8, district.DenizenSockets.Count);
            Assert.All(district.DenizenSockets, socket =>
                Assert.Contains("islander", socket.CompatibleDenizenTypes));
            Assert.Equal(50, district.EvolutionStages.Count);
            Assert.Equal("Building", district.EvolutionStages.Single(stage => stage.Order == 1).Kind);
            Assert.Equal("islander", district.EvolutionStages.Single(stage => stage.Order == 10).DenizenType);
        });
        Assert.Contains(template.Variants, variant =>
            variant.Category == "dwelling" && variant.SpriteKey == "buildings/hut-coral");
        Assert.Contains(template.Variants, variant =>
            variant.Category == "landmark" && variant.SpriteKey == "landmarks/sun-obelisk");
        Assert.Contains(template.Variants, variant =>
            variant.Category == "denizen:islander" && variant.SpriteKey == "denizens/islander-a");
    }

    [Fact]
    public async Task World_template_contract_is_complete_for_theme_agnostic_rendering()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var templates = await client.GetFromJsonAsync<List<WorldTemplateResponse>>("/api/world/templates");

        Assert.NotNull(templates);
        Assert.NotEmpty(templates);
        foreach (var template in templates)
        {
            AssertWorldTemplateCatalogueContract(template);
            AssertWorldTemplateAssetContract(template);
        }
    }

    [Fact]
    public async Task Active_world_starts_empty_with_area_district_bindings()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var era = await CreateEraAsync(
            client,
            xpPerLevel: 10,
            areas: [new CreateAreaRequest("Work", 1), new CreateAreaRequest("Social", 2)]);

        var world = await client.GetFromJsonAsync<WorldStateResponse>("/api/world");

        Assert.NotNull(world);
        Assert.Equal(era.Id, world.EraId);
        Assert.Equal(TemplateId, world.TemplateId);
        Assert.Equal(4, world.Districts.Count);

        var workDistrict = world.Districts.Single(district => district.AreaName == "Work");
        Assert.Equal(era.Areas.Single(area => area.Name == "Work").Id, workDistrict.AreaId);
        Assert.Equal(0, workDistrict.AreaLevel);
        Assert.Empty(workDistrict.BuiltPlots);
        Assert.Empty(workDistrict.Denizens);
    }

    [Fact]
    public async Task Level_up_evolves_the_bound_district_and_persists_denizens()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var era = await CreateEraAsync(
            client,
            xpPerLevel: 10,
            areas: [new CreateAreaRequest("Work", 1)],
            dailyHabits: [new CreateDailyHabitDraftRequest(1, "Ship something", 105)]);
        var habit = era.DailyHabits.Single();

        var completion = await CompleteDailyAsync(client, habit.Id);
        Assert.Equal(10, completion.AreaLevel);
        Assert.True(completion.LevelChanged);

        var world = await client.GetFromJsonAsync<WorldStateResponse>("/api/world");

        Assert.NotNull(world);
        var district = Assert.Single(world.Districts, item => item.AreaName == "Work");
        Assert.Equal(10, district.AreaLevel);
        Assert.Equal(9, district.BuiltPlots.Count);

        Assert.All(district.BuiltPlots, plot =>
        {
            Assert.NotEqual(Guid.Empty, plot.PlotId);
            Assert.NotEqual(Guid.Empty, plot.VariantId);
            Assert.False(string.IsNullOrWhiteSpace(plot.SpriteKey));
        });

        var denizen = Assert.Single(district.Denizens);
        Assert.Equal("islander", denizen.DenizenType);
        Assert.Equal(1, denizen.Count);
    }

    [Fact]
    public async Task Uncompleting_reverts_world_state_to_the_resulting_level()
    {
        using var factory = new BelfalasApiFactory();
        var client = factory.CreateClient();

        var era = await CreateEraAsync(
            client,
            xpPerLevel: 10,
            areas: [new CreateAreaRequest("Work", 1)],
            dailyHabits: [new CreateDailyHabitDraftRequest(1, "Ship something", 105)]);
        var habit = era.DailyHabits.Single();

        await CompleteDailyAsync(client, habit.Id);

        var response = await client.DeleteAsync($"/api/quests/daily/{habit.Id}/complete");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var world = await client.GetFromJsonAsync<WorldStateResponse>("/api/world");
        Assert.NotNull(world);
        var district = Assert.Single(world.Districts, item => item.AreaName == "Work");
        Assert.Equal(0, district.AreaLevel);
        Assert.Empty(district.BuiltPlots);
        Assert.Empty(district.Denizens);
    }

    private static async Task<EraDetailResponse> CreateEraAsync(
        HttpClient client,
        int xpPerLevel,
        IReadOnlyList<CreateAreaRequest> areas,
        IReadOnlyList<CreateDailyHabitDraftRequest>? dailyHabits = null)
    {
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var request = new CreateEraRequest(
            "Test Era",
            startDate,
            Weeks: 50,
            TemplateId,
            areas,
            dailyHabits,
            WeeklyGoals: null,
            xpPerLevel);

        var response = await client.PostAsJsonAsync("/api/eras", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var era = await response.Content.ReadFromJsonAsync<EraDetailResponse>();
        Assert.NotNull(era);
        return era;
    }

    private static async Task<QuestCompletionResponse> CompleteDailyAsync(HttpClient client, Guid habitId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/quests/daily/{habitId}/complete",
            new CompleteDailyQuestRequest(default));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QuestCompletionResponse>();
        Assert.NotNull(result);
        return result;
    }

    private static void AssertWorldTemplateCatalogueContract(WorldTemplateResponse template)
    {
        Assert.False(string.IsNullOrWhiteSpace(template.Id));
        Assert.False(string.IsNullOrWhiteSpace(template.Theme));
        Assert.False(string.IsNullOrWhiteSpace(template.Name));
        Assert.DoesNotContain('\\', template.Render.AssetBasePath);
        Assert.False(template.Render.AssetBasePath.EndsWith('/'));
        Assert.False(string.IsNullOrWhiteSpace(template.Render.AtlasKey));
        Assert.True(template.Render.TileWidth > 0);
        Assert.True(template.Render.TileHeight > 0);
        Assert.True(template.Render.MapWidth > 0);
        Assert.True(template.Render.MapHeight > 0);
        Assert.True(template.Render.CameraBounds.MaxX > template.Render.CameraBounds.MinX);
        Assert.True(template.Render.CameraBounds.MaxY > template.Render.CameraBounds.MinY);

        Assert.Equal(
            Enumerable.Range(0, template.Districts.Count).ToArray(),
            template.Districts.Select(district => district.Slot).Order().ToArray());

        var categoryContracts = template.Categories.Select(category => category.Category).ToHashSet();
        var variantCategories = template.Variants.Select(variant => variant.Category).ToHashSet();
        var atlasSpriteKeys = template.Variants.Select(variant => variant.SpriteKey).ToHashSet();

        foreach (var category in template.Categories)
        {
            Assert.False(category.Category.StartsWith("denizen:", StringComparison.Ordinal));
            Assert.True(category.FootprintWidth > 0);
            Assert.True(category.FootprintHeight > 0);
            Assert.InRange(category.AnchorX, 0, 1);
            Assert.InRange(category.AnchorY, 0, 1);
            Assert.Contains(category.Category, variantCategories);
        }

        foreach (var variant in template.Variants)
        {
            Assert.False(string.IsNullOrWhiteSpace(variant.SpriteKey));
            Assert.DoesNotContain('\\', variant.SpriteKey);
            Assert.True(
                categoryContracts.Contains(variant.Category) ||
                variant.Category.StartsWith("denizen:", StringComparison.Ordinal));
        }

        foreach (var district in template.Districts)
        {
            Assert.Equal(
                Enumerable.Range(1, 50).ToArray(),
                district.EvolutionStages.Select(stage => stage.Order).Order().ToArray());

            foreach (var plot in district.Plots)
            {
                Assert.Contains(plot.Category, categoryContracts);
                Assert.InRange(plot.PositionX, 0, template.Render.MapWidth - 1);
                Assert.InRange(plot.PositionY, 0, template.Render.MapHeight - 1);
            }

            var denizenTypesReferencedByStages = district.EvolutionStages
                .Where(stage => stage.Kind == "Denizen")
                .Select(stage => stage.DenizenType)
                .OfType<string>()
                .ToHashSet();

            foreach (var socket in district.DenizenSockets)
            {
                Assert.InRange(socket.PositionX, 0, template.Render.MapWidth - 1);
                Assert.InRange(socket.PositionY, 0, template.Render.MapHeight - 1);
                Assert.InRange(socket.AnchorX, 0, 1);
                Assert.InRange(socket.AnchorY, 0, 1);
                Assert.NotEmpty(socket.CompatibleDenizenTypes);

                foreach (var denizenType in socket.CompatibleDenizenTypes)
                {
                    Assert.Contains($"denizen:{denizenType}", variantCategories);
                }
            }

            foreach (var denizenType in denizenTypesReferencedByStages)
            {
                Assert.Contains($"denizen:{denizenType}", variantCategories);
                Assert.Contains(district.DenizenSockets, socket => socket.CompatibleDenizenTypes.Contains(denizenType));
            }
        }

        Assert.Equal(atlasSpriteKeys.Count, template.Variants.Count);
    }

    private static void AssertWorldTemplateAssetContract(WorldTemplateResponse template)
    {
        var assetDirectory = GetTemplateAssetDirectory(template);
        var mapPath = Path.Combine(assetDirectory, "map.json");
        var atlasPath = Path.Combine(assetDirectory, $"{template.Render.AtlasKey}.json");

        Assert.True(File.Exists(mapPath), $"Missing base map asset: {mapPath}");
        Assert.True(File.Exists(atlasPath), $"Missing atlas metadata asset: {atlasPath}");

        using var mapJson = JsonDocument.Parse(File.ReadAllText(mapPath));
        var map = mapJson.RootElement;
        Assert.Equal(template.Id, map.GetProperty("id").GetString());
        Assert.Equal(template.Render.TileWidth, map.GetProperty("tileWidth").GetInt32());
        Assert.Equal(template.Render.TileHeight, map.GetProperty("tileHeight").GetInt32());
        Assert.Equal(template.Render.MapWidth, map.GetProperty("width").GetInt32());
        Assert.Equal(template.Render.MapHeight, map.GetProperty("height").GetInt32());
        Assert.Equal(template.Render.OriginX, map.GetProperty("origin").GetProperty("x").GetInt32());
        Assert.Equal(template.Render.OriginY, map.GetProperty("origin").GetProperty("y").GetInt32());

        var terrainLegend = map.GetProperty("terrainLegend")
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
        var rows = map.GetProperty("rows").EnumerateArray().ToArray();
        Assert.Equal(template.Render.MapHeight, rows.Length);

        foreach (var row in rows)
        {
            var tokens = row.EnumerateArray().Select(token => token.GetString() ?? string.Empty).ToArray();
            Assert.Equal(template.Render.MapWidth, tokens.Length);
            Assert.All(tokens, token => Assert.Contains(token, terrainLegend.Keys));
        }

        using var atlasJson = JsonDocument.Parse(File.ReadAllText(atlasPath));
        var atlas = atlasJson.RootElement;
        var atlasImage = atlas.GetProperty("meta").GetProperty("image").GetString();
        Assert.False(string.IsNullOrWhiteSpace(atlasImage));
        Assert.True(File.Exists(Path.Combine(assetDirectory, atlasImage)), $"Missing atlas image: {atlasImage}");

        var frames = atlas.GetProperty("frames").EnumerateObject().Select(property => property.Name).ToHashSet();
        foreach (var spriteKey in terrainLegend.Values.Concat(template.Variants.Select(variant => variant.SpriteKey)))
        {
            Assert.Contains(spriteKey, frames);
        }
    }

    private static string GetTemplateAssetDirectory(WorldTemplateResponse template)
    {
        var repositoryRoot = FindRepositoryRoot();
        var relativeAssetPath = template.Render.AssetBasePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(repositoryRoot, "src", "frontend", "public", relativeAssetPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var frontendPublic = Path.Combine(directory.FullName, "src", "frontend", "public");
            if (Directory.Exists(frontendPublic))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from the test output directory.");
    }
}
