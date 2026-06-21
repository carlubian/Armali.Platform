import { useQuery } from '@tanstack/react-query'

import { processesApi } from '@/app/api/processes'

import { processesKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useProcessCategories() {
  return useQuery({
    queryKey: processesKeys.categories(),
    queryFn: ({ signal }) => processesApi.categories(signal),
    staleTime: catalogStaleTime,
  })
}

export { processesKeys }
