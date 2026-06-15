namespace Segaris.Api.Modules.Capex.Contracts;

/// <summary>
/// Frozen request contract for creating or updating a Capex category
/// (<c>POST /api/capex/categories</c> and <c>PUT /api/capex/categories/{id}</c>).
/// The name is trimmed and unique case-insensitively within the category catalog;
/// the server is authoritative for ordering.
///
/// Category move and replace-and-delete reuse the shared catalog request shapes
/// (<c>CatalogMoveRequest</c> and <c>CatalogReplacementRequest</c>) published by
/// Configuration, since the wire contract is identical. Categories are required
/// and replace-only: clearing references and exchange rates are never valid.
/// </summary>
internal sealed record CapexCategoryRequest(string? Name);
