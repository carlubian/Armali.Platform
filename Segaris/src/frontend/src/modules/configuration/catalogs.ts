import { capexApi, capexCategoriesManagementApi } from '@/app/api/capex'
import type { CatalogManagementClient } from '@/app/api/catalogs'
import {
  assetCategoriesManagementApi,
  assetLocationsManagementApi,
  assetsApi,
} from '@/app/api/assets'
import {
  clothesApi,
  clothingCategoriesManagementApi,
  clothingColorsManagementApi,
} from '@/app/api/clothes'
import { configurationApi, configurationManagementApi } from '@/app/api/configuration'
import {
  inventoryApi,
  inventoryCategoriesManagementApi,
  inventoryLocationsManagementApi,
} from '@/app/api/inventory'
import { opexApi, opexCategoriesManagementApi } from '@/app/api/opex'
import {
  travelApi,
  travelExpenseCategoriesManagementApi,
  travelTripTypesManagementApi,
} from '@/app/api/travel'
import { assetsKeys } from '@/modules/assets/contracts'
import { capexKeys, configurationKeys } from '@/modules/capex/queries'
import { clothesKeys } from '@/modules/clothes/contracts'
import { inventoryKeys } from '@/modules/inventory/queries'
import { maintenanceApi, maintenanceTypesManagementApi } from '@/app/api/maintenance'
import { maintenanceKeys } from '@/modules/maintenance/contracts'
import { opexKeys } from '@/modules/opex/contracts'
import { travelKeys } from '@/modules/travel/contracts'

/** The administrative catalog keys backing the Configuration table and dialogs. */
export type CatalogKey =
  | 'suppliers'
  | 'costCenters'
  | 'currencies'
  | 'categories'
  | 'opexCategories'
  | 'inventoryCategories'
  | 'inventoryLocations'
  | 'travelTripTypes'
  | 'travelExpenseCategories'
  | 'clothingCategories'
  | 'clothingColors'
  | 'assetCategories'
  | 'assetLocations'
  | 'maintenanceTypes'

/** Flat top-level sections of the Configuration experience. */
export type CatalogSectionId =
  | 'global'
  | 'capex'
  | 'opex'
  | 'inventory'
  | 'travel'
  | 'clothes'
  | 'assets'
  | 'maintenance'

/**
 * Structural row shared by the catalog table and dialogs. Every catalog row has
 * `id`, `name`, and `sortOrder`; only currencies add `code`.
 */
export interface CatalogRow {
  id: number
  name: string
  sortOrder: number
  code?: string
  colorValue?: string
}

/** Create/update body shape covering every catalog. */
export interface CatalogWriteBody {
  name: string
  code?: string
  colorValue?: string
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
  /** Clothing colours carry an editable hex colour value. */
  hasColorValue?: boolean
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
  dependentKeys: [
    capexKeys.entries(),
    inventoryKeys.items(),
    inventoryKeys.orders(),
    travelKeys.trips(),
  ],
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
  dependentKeys: [capexKeys.entries(), travelKeys.trips()],
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
  dependentKeys: [capexKeys.entries(), inventoryKeys.orders(), travelKeys.trips()],
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

export const travelTripTypesDescriptor: CatalogDescriptor = {
  key: 'travelTripTypes',
  section: 'travel',
  urlSlug: 'trip-types',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: travelKeys.tripTypes(),
  dependentKeys: [travelKeys.trips()],
  read: (signal) => travelApi.tripTypes(signal),
  management: asDescriptorClient(travelTripTypesManagementApi),
}

export const travelExpenseCategoriesDescriptor: CatalogDescriptor = {
  key: 'travelExpenseCategories',
  section: 'travel',
  urlSlug: 'expense-categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: travelKeys.expenseCategories(),
  dependentKeys: [travelKeys.trips()],
  read: (signal) => travelApi.expenseCategories(signal),
  management: asDescriptorClient(travelExpenseCategoriesManagementApi),
}

export const clothingCategoriesDescriptor: CatalogDescriptor = {
  key: 'clothingCategories',
  section: 'clothes',
  urlSlug: 'categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: clothesKeys.categories(),
  dependentKeys: [clothesKeys.garments()],
  read: (signal) => clothesApi.categories(signal),
  management: asDescriptorClient(clothingCategoriesManagementApi),
}

export const clothingColorsDescriptor: CatalogDescriptor = {
  key: 'clothingColors',
  section: 'clothes',
  urlSlug: 'colors',
  hasCode: false,
  hasColorValue: true,
  canClear: true,
  isCurrency: false,
  queryKey: clothesKeys.colors(),
  dependentKeys: [clothesKeys.garments()],
  read: (signal) => clothesApi.colors(signal),
  management: asDescriptorClient(clothingColorsManagementApi),
}

export const assetCategoriesDescriptor: CatalogDescriptor = {
  key: 'assetCategories',
  section: 'assets',
  urlSlug: 'categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: assetsKeys.categories(),
  dependentKeys: [assetsKeys.assets()],
  read: (signal) => assetsApi.categories(signal),
  management: asDescriptorClient(assetCategoriesManagementApi),
}

export const assetLocationsDescriptor: CatalogDescriptor = {
  key: 'assetLocations',
  section: 'assets',
  urlSlug: 'locations',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: assetsKeys.locations(),
  dependentKeys: [assetsKeys.assets()],
  read: (signal) => assetsApi.locations(signal),
  management: asDescriptorClient(assetLocationsManagementApi),
}

export const maintenanceTypesDescriptor: CatalogDescriptor = {
  key: 'maintenanceTypes',
  section: 'maintenance',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: maintenanceKeys.types(),
  dependentKeys: [maintenanceKeys.tasks()],
  read: (signal) => maintenanceApi.types(signal),
  management: asDescriptorClient(maintenanceTypesManagementApi),
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

/** Travel-section catalogs, in tab order. */
export const travelCatalogs: readonly CatalogDescriptor[] = [
  travelTripTypesDescriptor,
  travelExpenseCategoriesDescriptor,
]

/** Clothes-section catalogs, in tab order. */
export const clothesCatalogs: readonly CatalogDescriptor[] = [
  clothingCategoriesDescriptor,
  clothingColorsDescriptor,
]

/** Assets-section catalogs, in tab order. */
export const assetsCatalogs: readonly CatalogDescriptor[] = [
  assetCategoriesDescriptor,
  assetLocationsDescriptor,
]

export const allCatalogs: readonly CatalogDescriptor[] = [
  ...globalCatalogs,
  categoriesDescriptor,
  opexCategoriesDescriptor,
  ...inventoryCatalogs,
  ...travelCatalogs,
  ...clothesCatalogs,
  ...assetsCatalogs,
  maintenanceTypesDescriptor,
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
    case 'travel':
      return travelCatalogs
    case 'clothes':
      return clothesCatalogs
    case 'assets':
      return assetsCatalogs
    case 'maintenance':
      return [maintenanceTypesDescriptor]
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
