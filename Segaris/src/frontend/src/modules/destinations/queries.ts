import { useQuery } from '@tanstack/react-query'

import { destinationsApi } from '@/app/api/destinations'

import { destinationsKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useDestinationCategories() {
  return useQuery({
    queryKey: destinationsKeys.categories(),
    queryFn: ({ signal }) => destinationsApi.categories(signal),
    staleTime: catalogStaleTime,
  })
}

export function usePlaceCategories() {
  return useQuery({
    queryKey: destinationsKeys.placeCategories(),
    queryFn: ({ signal }) => destinationsApi.placeCategories(signal),
    staleTime: catalogStaleTime,
  })
}

export { destinationsKeys }
