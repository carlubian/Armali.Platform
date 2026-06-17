import { useQuery } from '@tanstack/react-query'

import {
  configurationApi,
  type CostCenter,
  type Currency,
  type Supplier,
} from '@/app/api/configuration'
import { travelApi } from '@/app/api/travel'

import { travelKeys } from './contracts'

export const configurationKeys = {
  all: ['configuration'] as const,
  suppliers: () => [...configurationKeys.all, 'suppliers'] as const,
  currencies: () => [...configurationKeys.all, 'currencies'] as const,
  costCenters: () => [...configurationKeys.all, 'costCenters'] as const,
}

const catalogStaleTime = 60 * 60 * 1000

export function useTravelTripTypes() {
  return useQuery({
    queryKey: travelKeys.tripTypes(),
    queryFn: ({ signal }) => travelApi.tripTypes(signal),
    staleTime: catalogStaleTime,
  })
}

export function useTravelExpenseCategories() {
  return useQuery({
    queryKey: travelKeys.expenseCategories(),
    queryFn: ({ signal }) => travelApi.expenseCategories(signal),
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

export function useCostCenters() {
  return useQuery<CostCenter[]>({
    queryKey: configurationKeys.costCenters(),
    queryFn: ({ signal }) => configurationApi.costCenters(signal),
    staleTime: catalogStaleTime,
  })
}

export { travelKeys }
