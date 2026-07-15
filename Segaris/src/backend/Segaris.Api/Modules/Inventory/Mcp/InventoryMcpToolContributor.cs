using Segaris.Api.Platform.Mcp;

namespace Segaris.Api.Modules.Inventory.Mcp;

internal sealed class InventoryMcpToolContributor : ISegarisMcpToolContributor
{
    public IReadOnlyList<SegarisMcpToolContract> Tools { get; } =
    [
        new(
            SegarisMcpToolNames.InventorySearchItems,
            [
                Parameter("search", "string", false, "Free-text item search."),
                Parameter("status", "string", false, "Item status filter."),
                Parameter("categoryId", "integer", false, "Category identifier filter."),
                Parameter("locationId", "integer", false, "Location identifier filter."),
                Parameter("supplierId", "integer", false, "Supplier identifier filter."),
                Parameter("visibility", "string", false, "Visibility filter."),
                Parameter("limit", "integer", false, "Maximum summaries to return.", SegarisMcpToolNames.DefaultListLimit.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ],
            IsReadOnly: true,
            IsIdempotent: true),
        new(
            SegarisMcpToolNames.InventoryGetItem,
            [Parameter("itemId", "integer", true, "Inventory item identifier.")],
            IsReadOnly: true,
            IsIdempotent: true),
        new(
            SegarisMcpToolNames.InventoryListLocations,
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
