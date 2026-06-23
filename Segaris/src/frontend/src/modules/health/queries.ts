import { useQuery } from '@tanstack/react-query'

import { healthApi } from '@/app/api/health'

import { healthKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useDiseaseCategories() {
  return useQuery({
    queryKey: healthKeys.diseaseCategories(),
    queryFn: ({ signal }) => healthApi.diseaseCategories(signal),
    staleTime: catalogStaleTime,
  })
}

export function useMedicineCategories() {
  return useQuery({
    queryKey: healthKeys.medicineCategories(),
    queryFn: ({ signal }) => healthApi.medicineCategories(signal),
    staleTime: catalogStaleTime,
  })
}

export { healthKeys }
