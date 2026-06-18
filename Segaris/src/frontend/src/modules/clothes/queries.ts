import { useQuery } from '@tanstack/react-query'

import { clothesApi } from '@/app/api/clothes'

import { clothesKeys } from './contracts'

export function useClothingCategories() {
  return useQuery({
    queryKey: clothesKeys.categories(),
    queryFn: ({ signal }) => clothesApi.categories(signal),
  })
}

export function useClothingColors() {
  return useQuery({
    queryKey: clothesKeys.colors(),
    queryFn: ({ signal }) => clothesApi.colors(signal),
  })
}
