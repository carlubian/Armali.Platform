import { useQuery } from '@tanstack/react-query'

import { maintenanceApi, type MaintenanceTaskListQuery } from '@/app/api/maintenance'

import { maintenanceKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useMaintenanceTypes() {
  return useQuery({
    queryKey: maintenanceKeys.types(),
    queryFn: ({ signal }) => maintenanceApi.types(signal),
    staleTime: catalogStaleTime,
  })
}

export function useMaintenanceTaskList(query: MaintenanceTaskListQuery) {
  return useQuery({
    queryKey: maintenanceKeys.taskList(query),
    queryFn: ({ signal }) => maintenanceApi.listTasks(query, signal),
  })
}

export { maintenanceKeys }
