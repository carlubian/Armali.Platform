using Segaris.Api.Platform.Mcp;

namespace Segaris.Api.Modules.Capex.Mcp;

internal sealed class CapexMcpToolContributor : ISegarisMcpToolContributor
{
    public IReadOnlyList<SegarisMcpToolContract> Tools { get; } =
    [
        new(
            SegarisMcpToolNames.CapexSearchEntries,
            [
                Parameter("search", "string", false, "Free-text entry search."),
                Parameter("from", "date", false, "Inclusive due-date lower bound."),
                Parameter("to", "date", false, "Inclusive due-date upper bound."),
                Parameter("type", "string", false, "Movement type filter."),
                Parameter("status", "string", false, "Entry status filter."),
                Parameter("categoryId", "integer", false, "Category identifier filter."),
                Parameter("supplierId", "integer", false, "Supplier identifier filter."),
                Parameter("costCenterId", "integer", false, "Cost-center identifier filter."),
                Parameter("currencyId", "integer", false, "Currency identifier filter."),
                Parameter("visibility", "string", false, "Visibility filter."),
                Parameter("limit", "integer", false, "Maximum summaries to return.", SegarisMcpToolNames.DefaultListLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ],
            IsReadOnly: true,
            IsIdempotent: true),
        new(
            SegarisMcpToolNames.CapexGetEntry,
            [Parameter("entryId", "integer", true, "Capex entry identifier.")],
            IsReadOnly: true,
            IsIdempotent: true),
        new(
            SegarisMcpToolNames.CapexListCategories,
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
