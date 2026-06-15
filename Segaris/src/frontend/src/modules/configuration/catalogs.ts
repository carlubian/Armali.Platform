import { capexApi, capexCategoriesManagementApi } from '@/app/api/capex'
import type { CatalogManagementClient } from '@/app/api/catalogs'
import { configurationApi, configurationManagementApi } from '@/app/api/configuration'
import { capexKeys, configurationKeys } from '@/modules/capex/queries'

/** The four known administrative catalogs. Not a runtime-defined set. */
export type CatalogKey = 'suppliers' | 'costCenters' | 'currencies' | 'categories'

/** Flat top-level sections of the Configuration experience. */
export type CatalogSectionId = 'global' | 'capex'

/**
 * Structural row shared by the catalog table and dialogs. Every catalog row has
 * `id`, `name`, and `sortOrder`; only currencies add `code`.
 */
export interface CatalogRow {
  id: number
  name: string
  sortOrder: number
  code?: string
}

/** Create/update body shape covering every catalog (currency adds `code`). */
export interface CatalogWriteBody {
  name: string
  code?: string
}

export interface CatalogDescriptor {
  key: CatalogKey
  section: CatalogSectionId
  /**
   * Slug used in `?catalog=` for Global tabs. Undefined for the single-catalog
   * Capex section, which has no Global tab.
   */
  urlSlug?: string
  /** Currencies carry an editable three-letter display code. */
  hasCode: boolean
  /** Optional references may be cleared to null (suppliers and cost centres). */
  canClear: boolean
  /** Currency carries the exchange-rate conversion deletion path. */
  isCurrency: boolean
  /** Existing read cache key, shared with the business forms that consume it. */
  queryKey: readonly unknown[]
  read: (signal?: AbortSignal) => Promise<CatalogRow[]>
  management: CatalogManagementClient<CatalogRow, CatalogWriteBody>
}

// The management clients are typed with their exact create/update bodies; the
// wire body is structurally `{ name; code? }`, so they are surfaced here under
// the unified descriptor type. Non-currency dialogs never send `code`.
const asDescriptorClient = <TRow extends CatalogRow, TWrite>(
  client: CatalogManagementClient<TRow, TWrite>,
): CatalogManagementClient<CatalogRow, CatalogWriteBody> =>
  client as unknown as CatalogManagementClient<CatalogRow, CatalogWriteBody>

export const suppliersDescriptor: CatalogDescriptor = {
  key: 'suppliers',
  section: 'global',
  urlSlug: 'suppliers',
  hasCode: false,
  canClear: true,
  isCurrency: false,
  queryKey: configurationKeys.suppliers(),
  read: (signal) => configurationApi.suppliers(signal),
  management: asDescriptorClient(configurationManagementApi.suppliers),
}

export const costCentersDescriptor: CatalogDescriptor = {
  key: 'costCenters',
  section: 'global',
  urlSlug: 'cost-centers',
  hasCode: false,
  canClear: true,
  isCurrency: false,
  queryKey: configurationKeys.costCenters(),
  read: (signal) => configurationApi.costCenters(signal),
  management: asDescriptorClient(configurationManagementApi.costCenters),
}

export const currenciesDescriptor: CatalogDescriptor = {
  key: 'currencies',
  section: 'global',
  urlSlug: 'currencies',
  hasCode: true,
  canClear: false,
  isCurrency: true,
  queryKey: configurationKeys.currencies(),
  read: (signal) => configurationApi.currencies(signal),
  management: asDescriptorClient(configurationManagementApi.currencies),
}

export const categoriesDescriptor: CatalogDescriptor = {
  key: 'categories',
  section: 'capex',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: capexKeys.categories(),
  read: (signal) => capexApi.categories(signal),
  management: asDescriptorClient(capexCategoriesManagementApi),
}

/** Global-section catalogs, in tab order. */
export const globalCatalogs: readonly CatalogDescriptor[] = [
  suppliersDescriptor,
  costCentersDescriptor,
  currenciesDescriptor,
]

export const allCatalogs: readonly CatalogDescriptor[] = [
  ...globalCatalogs,
  categoriesDescriptor,
]

/** The default Global tab a bare or unknown route falls back to. */
export const defaultGlobalSlug = suppliersDescriptor.urlSlug as string

/** Resolves a Global `?catalog=` slug to its descriptor, if it is known. */
export function globalCatalogBySlug(
  slug: string | null,
): CatalogDescriptor | undefined {
  return globalCatalogs.find((catalog) => catalog.urlSlug === slug)
}
