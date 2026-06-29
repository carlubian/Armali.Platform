using System.Net;
using System.Net.Http.Json;
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
        var template = Assert.Single(templates);
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
}
