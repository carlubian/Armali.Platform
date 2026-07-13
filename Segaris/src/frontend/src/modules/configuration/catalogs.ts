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
  firebirdApi,
  personCategoriesManagementApi,
  usernamePlatformsManagementApi,
} from '@/app/api/firebird'
import { gamesApi, gamesManagementApi, type GamePlatform } from '@/app/api/games'
import {
  healthApi,
  diseaseCategoriesManagementApi,
  medicineCategoriesManagementApi,
} from '@/app/api/health'
import { healthKeys } from '@/modules/health/contracts'
import {
  inventoryApi,
  inventoryCategoriesManagementApi,
  inventoryLocationsManagementApi,
} from '@/app/api/inventory'
import { opexApi, opexCategoriesManagementApi } from '@/app/api/opex'
import { processCategoriesManagementApi, processesApi } from '@/app/api/processes'
import { recipeCategoriesManagementApi, recipesApi } from '@/app/api/recipes'
import {
  travelApi,
  travelExpenseCategoriesManagementApi,
  travelTripTypesManagementApi,
} from '@/app/api/travel'
import {
  wellnessApi,
  wellnessTaskNameMaxLength,
  wellnessTasksManagementApi,
  type WellnessCategory,
} from '@/app/api/wellness'
import {
  destinationCategoriesManagementApi,
  destinationsApi,
  placeCategoriesManagementApi,
} from '@/app/api/destinations'
import { assetsKeys } from '@/modules/assets/contracts'
import { capexKeys, configurationKeys } from '@/modules/capex/queries'
import { clothesKeys } from '@/modules/clothes/contracts'
import { destinationsKeys } from '@/modules/destinations/contracts'
import { firebirdKeys } from '@/modules/firebird/contracts'
import { gamesKeys } from '@/modules/games/contracts'
import { inventoryKeys } from '@/modules/inventory/queries'
import { maintenanceApi, maintenanceTypesManagementApi } from '@/app/api/maintenance'
import { maintenanceKeys } from '@/modules/maintenance/contracts'
import { opexKeys } from '@/modules/opex/contracts'
import { processesKeys } from '@/modules/processes/contracts'
import { recipesKeys } from '@/modules/recipes/contracts'
import { travelKeys } from '@/modules/travel/contracts'
import { wellnessKeys } from '@/modules/wellness/contracts'

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
  | 'destinationCategories'
  | 'placeCategories'
  | 'clothingCategories'
  | 'clothingColors'
  | 'assetCategories'
  | 'assetLocations'
  | 'maintenanceTypes'
  | 'processCategories'
  | 'recipeCategories'
  | 'diseaseCategories'
  | 'medicineCategories'
  | 'personCategories'
  | 'usernamePlatforms'
  | 'games'
  | 'wellnessTasks'

/** Flat top-level sections of the Configuration experience. */
export type CatalogSectionId =
  | 'global'
  | 'capex'
  | 'opex'
  | 'inventory'
  | 'travel'
  | 'destinations'
  | 'clothes'
  | 'assets'
  | 'maintenance'
  | 'firebird'
  | 'projects'
  | 'processes'
  | 'recipes'
  | 'health'
  | 'games'
  | 'wellness'

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
  /** Current exchange rate to EUR; only currencies carry it. `null` means a rate
   * has not been configured yet. */
  exchangeRateToEur?: number | null
  /** Fixed platform; only games carry it. */
  platform?: GamePlatform
  /** Fixed Wellness category; only Wellness tasks carry it. */
  category?: WellnessCategory
}

/** Create/update body shape covering every catalog. */
export interface CatalogWriteBody {
  name: string
  code?: string
  colorValue?: string
  exchangeRateToEur?: number
  platform?: GamePlatform
  category?: WellnessCategory
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
  /** Games carry a fixed platform enum, edited as a select and shown as a column. */
  hasPlatform?: boolean
  /** Wellness tasks carry a fixed healthy-habit category enum. */
  hasWellnessCategory?: boolean
  /** Optional per-catalog name length bound. Defaults to the shared 100. */
  nameMaxLength?: number
  /** Whether rows can be edited after creation. Defaults to true. */
  canEdit?: boolean
  /** Whether the table exposes server-backed order controls. Defaults to true. */
  canMove?: boolean
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

export const destinationCategoriesDescriptor: CatalogDescriptor = {
  key: 'destinationCategories',
  section: 'destinations',
  urlSlug: 'categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: destinationsKeys.categories(),
  dependentKeys: [destinationsKeys.destinations()],
  read: (signal) => destinationsApi.categories(signal),
  management: asDescriptorClient(destinationCategoriesManagementApi),
}

export const placeCategoriesDescriptor: CatalogDescriptor = {
  key: 'placeCategories',
  section: 'destinations',
  urlSlug: 'place-categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: destinationsKeys.placeCategories(),
  dependentKeys: [destinationsKeys.destinations()],
  read: (signal) => destinationsApi.placeCategories(signal),
  management: asDescriptorClient(placeCategoriesManagementApi),
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

export const processCategoriesDescriptor: CatalogDescriptor = {
  key: 'processCategories',
  section: 'processes',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: processesKeys.categories(),
  dependentKeys: [processesKeys.all],
  read: (signal) => processesApi.categories(signal),
  management: asDescriptorClient(processCategoriesManagementApi),
}

export const recipeCategoriesDescriptor: CatalogDescriptor = {
  key: 'recipeCategories',
  section: 'recipes',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: recipesKeys.categories(),
  dependentKeys: [recipesKeys.recipes(), recipesKeys.menus()],
  read: (signal) => recipesApi.categories(signal),
  management: asDescriptorClient(recipeCategoriesManagementApi),
}

export const diseaseCategoriesDescriptor: CatalogDescriptor = {
  key: 'diseaseCategories',
  section: 'health',
  urlSlug: 'disease-categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: healthKeys.diseaseCategories(),
  dependentKeys: [healthKeys.diseases()],
  read: (signal) => healthApi.diseaseCategories(signal),
  management: asDescriptorClient(diseaseCategoriesManagementApi),
}

export const medicineCategoriesDescriptor: CatalogDescriptor = {
  key: 'medicineCategories',
  section: 'health',
  urlSlug: 'medicine-categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: healthKeys.medicineCategories(),
  dependentKeys: [healthKeys.medicines()],
  read: (signal) => healthApi.medicineCategories(signal),
  management: asDescriptorClient(medicineCategoriesManagementApi),
}

export const personCategoriesDescriptor: CatalogDescriptor = {
  key: 'personCategories',
  section: 'firebird',
  urlSlug: 'person-categories',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: firebirdKeys.categories(),
  dependentKeys: [firebirdKeys.people()],
  read: (signal) => firebirdApi.categories(signal),
  management: asDescriptorClient(personCategoriesManagementApi),
}

export const usernamePlatformsDescriptor: CatalogDescriptor = {
  key: 'usernamePlatforms',
  section: 'firebird',
  urlSlug: 'username-platforms',
  hasCode: false,
  canClear: false,
  isCurrency: false,
  queryKey: firebirdKeys.platforms(),
  dependentKeys: [firebirdKeys.people()],
  read: (signal) => firebirdApi.platforms(signal),
  management: asDescriptorClient(usernamePlatformsManagementApi),
}

export const gamesDescriptor: CatalogDescriptor = {
  key: 'games',
  section: 'games',
  hasCode: false,
  hasPlatform: true,
  // Every playthrough requires a game, so a referenced game can only be replaced,
  // never cleared.
  canClear: false,
  isCurrency: false,
  queryKey: gamesKeys.games(),
  // A rename or replacement changes the game name and platform playthroughs show.
  dependentKeys: [gamesKeys.playthroughs()],
  read: (signal) => gamesApi.games(signal),
  management: asDescriptorClient(gamesManagementApi),
}

export const wellnessTasksDescriptor: CatalogDescriptor = {
  key: 'wellnessTasks',
  section: 'wellness',
  hasCode: false,
  hasWellnessCategory: true,
  nameMaxLength: wellnessTaskNameMaxLength,
  canEdit: false,
  canMove: false,
  canClear: false,
  isCurrency: false,
  queryKey: wellnessKeys.tasks(),
  dependentKeys: [wellnessKeys.today(), wellnessKeys.all],
  read: (signal) => wellnessApi.tasks(signal),
  management: asDescriptorClient(wellnessTasksManagementApi),
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

/** Destinations-section catalogs, in tab order. */
export const destinationsCatalogs: readonly CatalogDescriptor[] = [
  destinationCategoriesDescriptor,
  placeCategoriesDescriptor,
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

/** Firebird-section catalogs, in tab order. */
export const firebirdCatalogs: readonly CatalogDescriptor[] = [
  personCategoriesDescriptor,
  usernamePlatformsDescriptor,
]

export const allCatalogs: readonly CatalogDescriptor[] = [
  ...globalCatalogs,
  categoriesDescriptor,
  opexCategoriesDescriptor,
  ...inventoryCatalogs,
  ...travelCatalogs,
  ...destinationsCatalogs,
  ...clothesCatalogs,
  ...assetsCatalogs,
  maintenanceTypesDescriptor,
  processCategoriesDescriptor,
  recipeCategoriesDescriptor,
  diseaseCategoriesDescriptor,
  medicineCategoriesDescriptor,
  ...firebirdCatalogs,
  gamesDescriptor,
  wellnessTasksDescriptor,
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
    case 'destinations':
      return destinationsCatalogs
    case 'clothes':
      return clothesCatalogs
    case 'assets':
      return assetsCatalogs
    case 'maintenance':
      return [maintenanceTypesDescriptor]
    case 'firebird':
      return firebirdCatalogs
    case 'projects':
      return []
    case 'processes':
      return [processCategoriesDescriptor]
    case 'recipes':
      return [recipeCategoriesDescriptor]
    case 'health':
      return [diseaseCategoriesDescriptor, medicineCategoriesDescriptor]
    case 'capex':
      return [categoriesDescriptor]
    case 'opex':
      return [opexCategoriesDescriptor]
    case 'games':
      return [gamesDescriptor]
    case 'wellness':
      return [wellnessTasksDescriptor]
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
