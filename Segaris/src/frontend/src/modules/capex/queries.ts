import { useQuery } from '@tanstack/react-query'

import { capexApi, type CapexEntryListQuery } from '@/app/api/capex'
import {
  configurationApi,
  type CostCenter,
  type Currency,
  type Supplier,
} from '@/app/api/configuration'

/**
 * Stable TanStack Query keys for Capex and the shared Configuration catalogs.
 * Seeded catalogs change rarely, so they are cached aggressively; entry lists
 * are keyed by the full query so navigation and filtering stay predictable.
 */
export const capexKeys = {
  all: ['capex'] as const,
  categories: () => [...capexKeys.all, 'categories'] as const,
  entries: () => [...capexKeys.all, 'entries'] as const,
  entryList: (query: CapexEntryListQuery) =>
    [...capexKeys.entries(), 'list', query] as const,
  entry: (entryId: number) => [...capexKeys.all, 'entry', entryId] as const,
  attachments: (entryId: number) =>
    [...capexKeys.all, 'entry', entryId, 'attachments'] as const,
}

export const configurationKeys = {
  all: ['configuration'] as const,
  suppliers: () => [...configurationKeys.all, 'suppliers'] as const,
  costCenters: () => [...configurationKeys.all, 'cost-centers'] as const,
  currencies: () => [...configurationKeys.all, 'currencies'] as const,
}

// Seeded reference data is effectively static for the lifetime of a session.
const catalogStaleTime = 60 * 60 * 1000

export function useCapexCategories() {
  return useQuery({
    queryKey: capexKeys.categories(),
    queryFn: ({ signal }) => capexApi.categories(signal),
    staleTime: catalogStaleTime,
  })
}

export function useSuppliers() {
  return useQuery<Supplier[]>({
    queryKey: configurationKeys.suppliers(),
    queryFn: ({ signal }) => configurationApi.suppliers(signal),
    staleTime: catalogStaleTime,
  })
}

export function useCostCenters() {
  return useQuery<CostCenter[]>({
    queryKey: configurationKeys.costCenters(),
    queryFn: ({ signal }) => configurationApi.costCenters(signal),
    staleTime: catalogStaleTime,
  })
}

export function useCurrencies() {
  return useQuery<Currency[]>({
    queryKey: configurationKeys.currencies(),
    queryFn: ({ signal }) => configurationApi.currencies(signal),
    staleTime: catalogStaleTime,
  })
}
