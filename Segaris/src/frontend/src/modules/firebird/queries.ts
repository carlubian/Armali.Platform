import { useQuery } from '@tanstack/react-query'

import { firebirdApi } from '@/app/api/firebird'

import { firebirdKeys } from './contracts'

export function usePersonCategories() {
  return useQuery({
    queryKey: firebirdKeys.categories(),
    queryFn: ({ signal }) => firebirdApi.categories(signal),
  })
}

export function useUsernamePlatforms() {
  return useQuery({
    queryKey: firebirdKeys.platforms(),
    queryFn: ({ signal }) => firebirdApi.platforms(signal),
  })
}
