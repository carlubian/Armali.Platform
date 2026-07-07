import { useTranslation } from 'react-i18next'

import { gamePlatforms, type Game, type GamePlatform } from '@/app/api/games'
import {
  EntitySelectorDialog,
  type EntityQueryResult,
  type EntitySelectorColumn,
  type EntitySelectorFilter,
  type EntitySelectorLabels,
  type EntitySelectorState,
} from '@/components/entity-selection'
import { Badge } from '@/components/ui'

import { useGames } from './queries'

const defaultSort = { field: 'name', direction: 'asc' } as const

export interface GameEntitySelectorProps {
  currentGameId?: number | null
  onSelect: (game: Game) => void
  onClose: () => void
}

/**
 * Floating game-catalogue picker for the playthrough editor. The catalogue is a
 * small, module-owned list loaded whole, so search, the platform facet, sorting,
 * and pagination all run client-side over the cached rows.
 */
export function GameEntitySelector({
  currentGameId,
  onSelect,
  onClose,
}: GameEntitySelectorProps) {
  const { t } = useTranslation('games')

  const useEntities = (state: EntitySelectorState): EntityQueryResult<Game> => {
    const games = useGames()
    const all = games.data ?? []
    const search = state.search.trim().toLowerCase()
    const platform = state.filters.platform ?? ''
    const direction = state.sort?.direction ?? defaultSort.direction
    const field = state.sort?.field ?? defaultSort.field

    const filtered = all.filter((game) => {
      if (platform !== '' && game.platform !== platform) return false
      if (search !== '' && !game.name.toLowerCase().includes(search)) return false
      return true
    })

    const sorted = [...filtered].sort((a, b) => {
      let cmp =
        field === 'platform'
          ? t(`platform.${a.platform}`).localeCompare(t(`platform.${b.platform}`))
          : 0
      if (cmp === 0) cmp = a.name.localeCompare(b.name)
      return direction === 'asc' ? cmp : -cmp
    })

    const total = sorted.length
    const start = (state.page - 1) * state.pageSize
    const items = sorted.slice(start, start + state.pageSize)

    return {
      items,
      total,
      isLoading: games.isLoading,
      isFetching: games.isFetching,
      isError: games.isError,
      refetch: () => void games.refetch(),
    }
  }

  const columns: ReadonlyArray<EntitySelectorColumn<Game>> = [
    {
      id: 'name',
      header: t('selector.columns.game'),
      sortField: 'name',
      width: 'minmax(0, 2fr)',
      render: (game) => <strong>{game.name}</strong>,
    },
    {
      id: 'platform',
      header: t('selector.columns.platform'),
      sortField: 'platform',
      render: (game) => <Badge tone="azure">{t(`platform.${game.platform}`)}</Badge>,
    },
  ]

  const filters: EntitySelectorFilter[] = [
    {
      id: 'platform',
      label: t('selector.platform'),
      options: [
        { value: '', label: t('selector.allPlatforms') },
        ...gamePlatforms.map((platform: GamePlatform) => ({
          value: platform,
          label: t(`platform.${platform}`),
        })),
      ],
    },
  ]

  const labels: EntitySelectorLabels = {
    title: t('selector.title'),
    eyebrow: t('selector.eyebrow'),
    description: t('selector.description'),
    searchLabel: t('selector.searchLabel'),
    searchPlaceholder: t('selector.searchPlaceholder'),
    resultCount: (count) => t('selector.count', { count }),
    pageInfo: (start, end, total) => t('selector.range', { start, end, total }),
    clearAll: t('collection.filters.clearAll'),
    removeFilter: (label) => t('collection.filters.remove', { label }),
    selectAction: t('selector.select'),
    currentTag: t('selector.current'),
    cancel: t('selector.close'),
    close: t('selector.close'),
    loading: t('selector.loading'),
    refetching: t('selector.loading'),
    error: t('selector.loadError'),
    retry: t('selector.retry'),
    empty: t('selector.empty'),
    filteredEmpty: t('selector.empty'),
    clearFilters: t('collection.filters.clearAll'),
    previousPage: t('selector.previousPage'),
    nextPage: t('selector.nextPage'),
  }

  return (
    <EntitySelectorDialog<Game>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(game) => String(game.id)}
      currentId={currentGameId == null ? null : String(currentGameId)}
      onSelect={onSelect}
      onClose={onClose}
      defaultSort={defaultSort}
      pageSize={8}
      width={820}
    />
  )
}
