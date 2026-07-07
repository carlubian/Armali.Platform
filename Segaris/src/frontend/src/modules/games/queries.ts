import { useQuery } from '@tanstack/react-query'

import { gamesApi } from '@/app/api/games'

import { gamesKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useGames() {
  return useQuery({
    queryKey: gamesKeys.games(),
    queryFn: ({ signal }) => gamesApi.games(signal),
    staleTime: catalogStaleTime,
  })
}

export { gamesKeys }
