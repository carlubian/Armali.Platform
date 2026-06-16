import { useQuery } from '@tanstack/react-query'

import { opexApi } from '@/app/api/opex'
import {
  configurationApi,
  type CostCenter,
  type Currency,
  type Supplier,
} from '@/app/api/configuration'

import { opexKeys } from './contracts'

const configurationKeys = {
  all: ['configuration'] as const,
  suppliers: () => [...configurationKeys.all, 'suppliers'] as const,
  costCenters: () => [...configurationKeys.all, 'cost-centers'] as const,
  currencies: () => [...configurationKeys.all, 'currencies'] as const,
}

const catalogStaleTime = 60 * 60 * 1000

export function useOpexCategories() {
  return useQuery({
    queryKey: opexKeys.categories(),
    queryFn: ({ signal }) => opexApi.categories(signal),
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

export { opexKeys }
