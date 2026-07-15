namespace Segaris.Api.Platform.Mcp;

internal sealed class McpOptions
{
    public const string SectionName = "Segaris:Mcp";

    public const string EndpointPath = "/mcp";

    public bool Enabled { get; init; }
}
