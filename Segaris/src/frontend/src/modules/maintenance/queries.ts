import { useQuery } from '@tanstack/react-query'

import { maintenanceApi } from '@/app/api/maintenance'

import { maintenanceKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useMaintenanceTypes() {
  return useQuery({
    queryKey: maintenanceKeys.types(),
    queryFn: ({ signal }) => maintenanceApi.types(signal),
    staleTime: catalogStaleTime,
  })
}

export { maintenanceKeys }
