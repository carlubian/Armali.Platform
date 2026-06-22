import { useQuery } from '@tanstack/react-query'

import { recipesApi } from '@/app/api/recipes'

import { recipesKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useRecipeCategories() {
  return useQuery({
    queryKey: recipesKeys.categories(),
    queryFn: ({ signal }) => recipesApi.categories(signal),
    staleTime: catalogStaleTime,
  })
}

export { recipesKeys }
