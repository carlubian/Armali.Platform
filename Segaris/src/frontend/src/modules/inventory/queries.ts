import { useQuery } from '@tanstack/react-query'

import { configurationApi, type Currency, type Supplier } from '@/app/api/configuration'
import { inventoryApi } from '@/app/api/inventory'

import { inventoryKeys } from './contracts'

/**
 * Shared Configuration read keys. They match the keys the Capex, Opex, and
 * Configuration modules use so the supplier and currency catalogs resolve from a
 * single cache regardless of which module requested them first.
 */
export const configurationKeys = {
  all: ['configuration'] as const,
  suppliers: () => [...configurationKeys.all, 'suppliers'] as const,
  currencies: () => [...configurationKeys.all, 'currencies'] as const,
}

const catalogStaleTime = 60 * 60 * 1000

export function useInventoryCategories() {
  return useQuery({
    queryKey: inventoryKeys.categories(),
    queryFn: ({ signal }) => inventoryApi.categories(signal),
    staleTime: catalogStaleTime,
  })
}

export function useInventoryLocations() {
  return useQuery({
    queryKey: inventoryKeys.locations(),
    queryFn: ({ signal }) => inventoryApi.locations(signal),
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

export function useCurrencies() {
  return useQuery<Currency[]>({
    queryKey: configurationKeys.currencies(),
    queryFn: ({ signal }) => configurationApi.currencies(signal),
    staleTime: catalogStaleTime,
  })
}

export { inventoryKeys }
