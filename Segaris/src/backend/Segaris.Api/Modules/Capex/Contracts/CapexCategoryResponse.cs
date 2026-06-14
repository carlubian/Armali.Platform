namespace Segaris.Api.Modules.Capex.Contracts;

/// <summary>
/// Frozen response contract for <c>GET /api/capex/categories</c>.
/// <see cref="Code"/> is the stable identity; <see cref="Name"/> is localizable.
/// </summary>
internal sealed record CapexCategoryResponse(int Id, string Code, string Name);
