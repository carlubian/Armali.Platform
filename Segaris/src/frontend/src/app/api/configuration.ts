import { apiRequest } from './client'

/** Frozen catalog row from `GET /api/configuration/suppliers`. */
export interface Supplier {
  id: number
  code: string
  name: string
}

/** Frozen catalog row from `GET /api/configuration/cost-centers`. */
export interface CostCenter {
  id: number
  code: string
  name: string
}

/** Frozen catalog row from `GET /api/configuration/currencies`. `code` is ISO 4217. */
export interface Currency {
  id: number
  code: string
  name: string
}

export const configurationApi = {
  suppliers: (signal?: AbortSignal) =>
    apiRequest<Supplier[]>('/api/configuration/suppliers', { signal }),
  costCenters: (signal?: AbortSignal) =>
    apiRequest<CostCenter[]>('/api/configuration/cost-centers', { signal }),
  currencies: (signal?: AbortSignal) =>
    apiRequest<Currency[]>('/api/configuration/currencies', { signal }),
}
