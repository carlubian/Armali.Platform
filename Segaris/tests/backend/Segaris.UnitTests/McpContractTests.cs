using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Composition;
using Segaris.Api.Platform.Mcp;

namespace Segaris.UnitTests;

public sealed class McpContractTests
{
    [Fact]
    public void RegisteredMcpContributorsExposeTheWaveZeroToolSurface()
    {
        var services = new ServiceCollection()
            .AddSegarisModules(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();

        var tools = provider.GetServices<ISegarisMcpToolContributor>()
            .SelectMany(contributor => contributor.Tools)
            .ToArray();

        Assert.Equal(
            [
                SegarisMcpToolNames.CapexSearchEntries,
                SegarisMcpToolNames.CapexGetEntry,
                SegarisMcpToolNames.CapexListCategories,
                SegarisMcpToolNames.OpexSearchEntries,
                SegarisMcpToolNames.OpexGetEntry,
                SegarisMcpToolNames.OpexListCategories,
                SegarisMcpToolNames.InventorySearchItems,
                SegarisMcpToolNames.InventoryGetItem,
                SegarisMcpToolNames.InventoryListLocations,
            ],
            tools.Select(tool => tool.Name).ToArray());
    }

    [Fact]
    public void McpToolContractsUseAgentSafeConventions()
    {
        var services = new ServiceCollection()
            .AddSegarisModules(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();

        var tools = provider.GetServices<ISegarisMcpToolContributor>()
            .SelectMany(contributor => contributor.Tools)
            .ToArray();

        Assert.Equal(tools.Length, tools.Select(tool => tool.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(tools, tool =>
        {
            Assert.Matches("^[a-z]+_[a-z]+_[a-z]+$", tool.Name);
            Assert.True(tool.IsReadOnly);
            Assert.True(tool.IsIdempotent);
        });

        Assert.All(
            tools.Where(tool => tool.Name.Contains("_search_", StringComparison.Ordinal)),
            tool =>
            {
                var limit = Assert.Single(tool.Parameters, parameter => parameter.Name == "limit");
                Assert.Equal("integer", limit.Type);
                Assert.False(limit.IsRequired);
                Assert.Equal(SegarisMcpToolNames.DefaultListLimit.ToString(System.Globalization.CultureInfo.InvariantCulture), limit.DefaultValue);
                Assert.True(SegarisMcpToolNames.DefaultListLimit < SegarisMcpToolNames.MaximumListLimit);
                Assert.True(SegarisMcpToolNames.MaximumListLimit < 100);
            });
    }
}
