namespace Segaris.Api.Platform.Mcp;

internal sealed record SegarisMcpToolParameterContract(
    string Name,
    string Type,
    bool IsRequired,
    string? Description = null,
    string? DefaultValue = null);
