using Segaris.Api.Platform.Mcp;

namespace Segaris.Api.Modules.Opex.Mcp;

internal sealed class OpexMcpToolContributor : ISegarisMcpToolContributor
{
    public IReadOnlyList<SegarisMcpToolContract> Tools { get; } =
    [
        new(
            SegarisMcpToolNames.OpexSearchEntries,
            [
                Parameter("search", "string", false, "Free-text contract search."),
                Parameter("type", "string", false, "Movement type filter."),
                Parameter("status", "string", false, "Contract status filter."),
                Parameter("categoryId", "integer", false, "Category identifier filter."),
                Parameter("supplierId", "integer", false, "Supplier identifier filter."),
                Parameter("costCenterId", "integer", false, "Cost-center identifier filter."),
                Parameter("currencyId", "integer", false, "Currency identifier filter."),
                Parameter("frequency", "string", false, "Expected frequency filter."),
                Parameter("visibility", "string", false, "Visibility filter."),
                Parameter("limit", "integer", false, "Maximum summaries to return.", SegarisMcpToolNames.DefaultListLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ],
            IsReadOnly: true,
            IsIdempotent: true),
        new(
            SegarisMcpToolNames.OpexGetEntry,
            [Parameter("contractId", "integer", true, "Opex contract identifier.")],
            IsReadOnly: true,
            IsIdempotent: true),
        new(
            SegarisMcpToolNames.OpexListCategories,
            [],
            IsReadOnly: true,
            IsIdempotent: true),
    ];

    private static SegarisMcpToolParameterContract Parameter(
        string name,
        string type,
        bool isRequired,
        string description,
        string? defaultValue = null) =>
        new(name, type, isRequired, description, defaultValue);
}
