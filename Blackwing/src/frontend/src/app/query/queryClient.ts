import { QueryClient } from '@tanstack/react-query'

/**
 * Application-wide TanStack Query defaults.
 *
 * Blackwing browses a private library the user is actively editing, so
 * refetch-on-focus is off (it would reshuffle a gallery mid-scroll) and
 * mutations never retry: a duplicated upload or tag write is worse than a
 * visible failure. Queries retry once, which covers a transient network blip
 * without hammering the backend. When the typed API client lands with the
 * session phase this retry predicate narrows to transient errors only, the way
 * Segaris does it.
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        refetchOnWindowFocus: false,
        retry: 1,
      },
      mutations: {
        retry: false,
      },
    },
  })
}
