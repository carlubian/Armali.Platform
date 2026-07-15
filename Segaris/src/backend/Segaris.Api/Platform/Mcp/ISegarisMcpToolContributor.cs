namespace Segaris.Api.Platform.Mcp;

internal interface ISegarisMcpToolContributor
{
    IReadOnlyList<SegarisMcpToolContract> Tools { get; }
}
