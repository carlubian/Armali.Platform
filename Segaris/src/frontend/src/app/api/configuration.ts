import { catalogManagementClient, type CatalogManagementClient } from './catalogs'
import { apiRequest } from './client'

/** Frozen catalog row from `GET /api/configuration/suppliers`. */
export interface Supplier {
  id: number
  name: string
  sortOrder: number
}

/** Frozen catalog row from `GET /api/configuration/cost-centers`. */
export interface CostCenter {
  id: number
  name: string
  sortOrder: number
}

/** Frozen catalog row from `GET /api/configuration/currencies`. `code` is the
 * editable three-letter display code. `exchangeRateToEur` is the current rate to
 * EUR (`1 currency = exchangeRateToEur EUR`); it is `null` only for currencies that
 * predate Analytics and never received a rate. */
export interface Currency {
  id: number
  code: string
  name: string
  sortOrder: number
  exchangeRateToEur: number | null
}

/** Create/update body for suppliers and cost centres. */
export interface CatalogItemRequest {
  name: string
  colorValue?: string
}

/** Create/update body for currencies; `code` is a three-letter display code and
 * `exchangeRateToEur` is the current rate to EUR (fixed at `1` for EUR, required
 * and positive for other currencies). */
export interface CurrencyItemRequest {
  name: string
  code: string
  exchangeRateToEur: number
}

export const configurationApi = {
  suppliers: (signal?: AbortSignal) =>
    apiRequest<Supplier[]>('/api/configuration/suppliers', { signal }),
  costCenters: (signal?: AbortSignal) =>
    apiRequest<CostCenter[]>('/api/configuration/cost-centers', { signal }),
  currencies: (signal?: AbortSignal) =>
    apiRequest<Currency[]>('/api/configuration/currencies', { signal }),
}

/**
 * Administrator-only management clients for the shared catalogs. Reads stay on
 * {@link configurationApi} because business forms consume them; every method here
 * requires `Admin` and is antiforgery-protected on the server.
 */
export const configurationManagementApi: {
  suppliers: CatalogManagementClient<Supplier, CatalogItemRequest>
  costCenters: CatalogManagementClient<CostCenter, CatalogItemRequest>
  currencies: CatalogManagementClient<Currency, CurrencyItemRequest>
} = {
  suppliers: catalogManagementClient<Supplier, CatalogItemRequest>(
    '/api/configuration/suppliers',
  ),
  costCenters: catalogManagementClient<CostCenter, CatalogItemRequest>(
    '/api/configuration/cost-centers',
  ),
  currencies: catalogManagementClient<Currency, CurrencyItemRequest>(
    '/api/configuration/currencies',
  ),
}
