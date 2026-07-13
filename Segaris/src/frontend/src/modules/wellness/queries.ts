import { useQuery } from '@tanstack/react-query'

import { wellnessApi } from '@/app/api/wellness'

import { wellnessKeys } from './contracts'

export function useWellnessToday() {
  return useQuery({
    queryKey: wellnessKeys.today(),
    queryFn: ({ signal }) => wellnessApi.today(signal),
  })
}

export { wellnessKeys }
