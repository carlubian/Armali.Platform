import { useQuery } from '@tanstack/react-query'

import { assetsApi } from '@/app/api/assets'

import { assetsKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useAssetCategories() {
  return useQuery({
    queryKey: assetsKeys.categories(),
    queryFn: ({ signal }) => assetsApi.categories(signal),
    staleTime: catalogStaleTime,
  })
}

export function useAssetLocations() {
  return useQuery({
    queryKey: assetsKeys.locations(),
    queryFn: ({ signal }) => assetsApi.locations(signal),
    staleTime: catalogStaleTime,
  })
}

export { assetsKeys }
