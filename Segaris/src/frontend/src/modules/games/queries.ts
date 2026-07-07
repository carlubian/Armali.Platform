import { keepPreviousData, useQuery } from '@tanstack/react-query'

import { gamesApi, type PlaythroughListQuery } from '@/app/api/games'

import { gamesKeys } from './contracts'

const catalogStaleTime = 60 * 60 * 1000

export function useGames() {
  return useQuery({
    queryKey: gamesKeys.games(),
    queryFn: ({ signal }) => gamesApi.games(signal),
    staleTime: catalogStaleTime,
  })
}

export function usePlaythroughs(query: PlaythroughListQuery) {
  return useQuery({
    queryKey: gamesKeys.playthroughList(query),
    queryFn: ({ signal }) => gamesApi.listPlaythroughs(query, signal),
    placeholderData: keepPreviousData,
  })
}

export function usePlaythrough(playthroughId: number, enabled = true) {
  return useQuery({
    queryKey: gamesKeys.playthrough(playthroughId),
    queryFn: ({ signal }) => gamesApi.getPlaythrough(playthroughId, signal),
    enabled: enabled && Number.isFinite(playthroughId) && playthroughId > 0,
  })
}

export { gamesKeys }
