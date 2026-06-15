namespace Segaris.Api.Modules.Capex.Contracts;

/// <summary>
/// Frozen response contract for <c>GET /api/capex/categories</c>. Identity is
/// <see cref="Id"/>; <see cref="Name"/> is localizable and <see cref="SortOrder"/>
/// carries the deterministic catalog order.
/// </summary>
internal sealed record CapexCategoryResponse(int Id, string Name, int SortOrder);
