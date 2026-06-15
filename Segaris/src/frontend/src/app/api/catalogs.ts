import { apiRequest } from './client'

/**
 * Shared transport types and management client for the four known administrative
 * catalogs (suppliers, cost centres, currencies, and Capex categories). The wire
 * contract is identical across catalogs — only the create/update body differs —
 * so the per-row management routes are bound once here and reused by the
 * Configuration and Capex API modules. This is deliberate de-duplication of a
 * frozen contract, not a generic runtime catalog framework.
 */

/**
 * Privacy-neutral deletion-impact response from
 * `GET /api/{owner}/{catalog}/{id}/deletion-impact`. It never reveals counts,
 * record details, or owners — only which removal paths are currently available.
 */
export interface CatalogDeletionImpact {
  isReferenced: boolean
  canDeleteDirectly: boolean
  canClearReferences: boolean
  requiresExchangeRate: boolean
  hasReplacementCandidates: boolean
}

/** Allowed move directions; a boundary move is rejected by the server. */
export type CatalogMoveDirection = 'up' | 'down'

/**
 * Body for `POST /api/{owner}/{catalog}/{id}/replace-and-delete`.
 *
 * - `replacementId` is required unless `clearReferences` is true and must differ
 *   from the source.
 * - `clearReferences` clears optional references to null (suppliers and cost
 *   centres only) and is mutually exclusive with `replacementId`.
 * - `exchangeRate` is required only when deleting a referenced currency.
 */
export interface CatalogReplacementRequest {
  replacementId: number | null
  clearReferences: boolean
  exchangeRate: number | null
}

/** Typed management client for a single catalog collection path. */
export interface CatalogManagementClient<TRow, TWrite> {
  create: (body: TWrite, signal?: AbortSignal) => Promise<TRow>
  update: (id: number, body: TWrite, signal?: AbortSignal) => Promise<TRow>
  move: (
    id: number,
    direction: CatalogMoveDirection,
    signal?: AbortSignal,
  ) => Promise<void>
  deletionImpact: (id: number, signal?: AbortSignal) => Promise<CatalogDeletionImpact>
  remove: (id: number, signal?: AbortSignal) => Promise<void>
  replaceAndDelete: (
    id: number,
    body: CatalogReplacementRequest,
    signal?: AbortSignal,
  ) => Promise<void>
}

/**
 * Binds the six per-row management routes onto a catalog collection path such as
 * `/api/configuration/suppliers` or `/api/capex/categories`. Writes are protected
 * by antiforgery automatically inside {@link apiRequest}.
 */
export function catalogManagementClient<TRow, TWrite>(
  basePath: string,
): CatalogManagementClient<TRow, TWrite> {
  return {
    create: (body, signal) =>
      apiRequest<TRow>(basePath, {
        method: 'POST',
        body: JSON.stringify(body),
        signal,
      }),
    update: (id, body, signal) =>
      apiRequest<TRow>(`${basePath}/${id}`, {
        method: 'PUT',
        body: JSON.stringify(body),
        signal,
      }),
    move: (id, direction, signal) =>
      apiRequest<void>(`${basePath}/${id}/move`, {
        method: 'POST',
        body: JSON.stringify({ direction }),
        signal,
      }),
    deletionImpact: (id, signal) =>
      apiRequest<CatalogDeletionImpact>(`${basePath}/${id}/deletion-impact`, {
        signal,
      }),
    remove: (id, signal) =>
      apiRequest<void>(`${basePath}/${id}`, { method: 'DELETE', signal }),
    replaceAndDelete: (id, body, signal) =>
      apiRequest<void>(`${basePath}/${id}/replace-and-delete`, {
        method: 'POST',
        body: JSON.stringify(body),
        signal,
      }),
  }
}
