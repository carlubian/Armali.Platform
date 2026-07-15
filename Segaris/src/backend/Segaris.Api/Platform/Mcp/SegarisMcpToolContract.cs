namespace Segaris.Api.Platform.Mcp;

internal sealed record SegarisMcpToolContract(
    string Name,
    IReadOnlyList<SegarisMcpToolParameterContract> Parameters,
    bool IsReadOnly,
    bool IsIdempotent);
