import { QueryClient } from '@tanstack/react-query'

import { isApiError } from '@/app/api/errors'

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        refetchOnWindowFocus: false,
        retry: (failureCount, error) =>
          failureCount < 1 && isApiError(error) && error.kind === 'transient',
      },
      mutations: {
        retry: false,
      },
    },
  })
}
