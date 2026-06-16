import { capexApi, capexCategoriesManagementApi } from '@/app/api/capex'
import type { CatalogManagementClient } from '@/app/api/catalogs'
import { configurationApi, configurationManagementApi } from '@/app/api/configuration'
import {
  inventoryApi,
  inventoryCategoriesManagementApi,
  inventoryLocationsManagementApi,
} from '@/app/api/inventory'
import { opexApi, opexCategoriesManagementApi } from '@/app/api/opex'
import { capexKeys, configurationKeys } from '@/modules/capex/queries'
import { inventoryKeys } from '@/modules/inventory/queries'
import { opexKeys } from '@/modules/opex/contracts'

/** The administrative catalog keys backing the Configuration table and dialogs. */
export type CatalogKey =
  | 'suppliers'
  | 'costCenters'
  | 'currencies'
  | 'categories'
  | 'opexCategories'
  | 'inventoryCategories'
  | 'inventoryLocations'

/** Flat top-level sections of the Configuration experience. */
export type CatalogSectionId = 'global' | 'capex' | 'opex' | 'inventory'

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
   * Slug used in `?catalog=` for multi-catalog sections. Undefined for the
   * single-catalog Capex section, which has no per-catalog tab.
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
  /**
   * Business read caches to invalidate when a mutation can change the
   * denormalised names or references those records display (a rename, deletion,
   * or reference migration). A pure reorder leaves them untouched.
   */
  dependentKeys?: readonly (readonly unknown[])[]
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
  dependentKeys: [capexKeys.entries(), inventoryKeys.items(), inventoryKeys.orders()],
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
  dependentKeys: [capexKeys.entries()],
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
  dependentKeys: [capexKeys.entries(), inventoryKeys.orders()],
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
  dependentKeys: [capexKeys.entries()],
  read: (signal) => capexApi.categories(signal),
  management: asDescriptorClient(capexCategoriesManagementApi),
}

export const opexCategoriesDescriptor: CatalogDescriptor = {
  key: 'opexCategories',
  section: 'opex',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: opexKeys.categories(),
  dependentKeys: [opexKeys.contracts()],
  read: (signal) => opexApi.categories(signal),
  management: asDescriptorClient(opexCategoriesManagementApi),
}

export const inventoryCategoriesDescriptor: CatalogDescriptor = {
  key: 'inventoryCategories',
  section: 'inventory',
  urlSlug: 'categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: inventoryKeys.categories(),
  dependentKeys: [inventoryKeys.items()],
  read: (signal) => inventoryApi.categories(signal),
  management: asDescriptorClient(inventoryCategoriesManagementApi),
}

export const inventoryLocationsDescriptor: CatalogDescriptor = {
  key: 'inventoryLocations',
  section: 'inventory',
  urlSlug: 'locations',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: inventoryKeys.locations(),
  dependentKeys: [inventoryKeys.items()],
  read: (signal) => inventoryApi.locations(signal),
  management: asDescriptorClient(inventoryLocationsManagementApi),
}

/** Global-section catalogs, in tab order. */
export const globalCatalogs: readonly CatalogDescriptor[] = [
  suppliersDescriptor,
  costCentersDescriptor,
  currenciesDescriptor,
]

/** Inventory-section catalogs, in tab order. */
export const inventoryCatalogs: readonly CatalogDescriptor[] = [
  inventoryCategoriesDescriptor,
  inventoryLocationsDescriptor,
]

export const allCatalogs: readonly CatalogDescriptor[] = [
  ...globalCatalogs,
  categoriesDescriptor,
  opexCategoriesDescriptor,
  ...inventoryCatalogs,
]

/** The default Global tab a bare or unknown route falls back to. */
export const defaultGlobalSlug = suppliersDescriptor.urlSlug as string

/** Resolves a Global `?catalog=` slug to its descriptor, if it is known. */
export function globalCatalogBySlug(
  slug: string | null,
): CatalogDescriptor | undefined {
  return globalCatalogs.find((catalog) => catalog.urlSlug === slug)
}

/**
 * Multi-catalog sections expose a `?catalog=` tab per descriptor. Single-catalog
 * sections (Capex) render their one descriptor directly. Returns the descriptors
 * for a section in tab order, and the slug the section falls back to.
 */
export function sectionCatalogs(
  section: CatalogSectionId,
): readonly CatalogDescriptor[] {
  switch (section) {
    case 'global':
      return globalCatalogs
    case 'inventory':
      return inventoryCatalogs
    case 'capex':
      return [categoriesDescriptor]
    case 'opex':
      return [opexCategoriesDescriptor]
  }
}

export function defaultSlugForSection(section: CatalogSectionId): string | undefined {
  return sectionCatalogs(section)[0]?.urlSlug
}

export function catalogBySlug(
  section: CatalogSectionId,
  slug: string | null,
): CatalogDescriptor | undefined {
  return sectionCatalogs(section).find((catalog) => catalog.urlSlug === slug)
}
