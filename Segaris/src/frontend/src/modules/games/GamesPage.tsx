import { useQueryClient } from '@tanstack/react-query'
import {
  ArrowDown,
  ArrowRight,
  ArrowUp,
  Calendar,
  ChevronLeft,
  ChevronRight,
  Dices,
  Gamepad2,
  Globe,
  Lock,
  Monitor,
  Plus,
  Search,
  Shapes,
  Smartphone,
  Swords,
  Tag,
} from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import {
  gamePlatforms,
  gamesPageSizes,
  playthroughStatuses,
  type GamePlatform,
  type GamesPageSize,
  type GamesVisibility,
  type Playthrough,
  type PlaythroughSortField,
  type PlaythroughStatus,
  type PlaythroughSummary,
} from '@/app/api/games'
import { isApiError } from '@/app/api/errors'
import { useSession } from '@/app/session/SessionContext'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import {
  Badge,
  Button,
  Input,
  Select,
  Spinner,
  Toast,
  type BadgeTone,
} from '@/components/ui'

import { PlaythroughDialog } from './PlaythroughDialog'
import {
  activePlaythroughFilterCount,
  useGamesCollectionState,
  type PlaythroughFilterPatch,
  type PlaythroughListState,
} from './gamesState'
import { gamesKeys, useGames, usePlaythroughs } from './queries'

import './GamesPage.css'

const sortFields: readonly PlaythroughSortField[] = [
  'name',
  'game',
  'startDate',
  'status',
  'progress',
]

const platformIcon: Record<GamePlatform, ReactNode> = {
  PC: <Monitor size={24} aria-hidden="true" />,
  Console: <Gamepad2 size={24} aria-hidden="true" />,
  Mobile: <Smartphone size={24} aria-hidden="true" />,
  BoardGame: <Dices size={24} aria-hidden="true" />,
  TabletopRpg: <Swords size={24} aria-hidden="true" />,
  Other: <Shapes size={24} aria-hidden="true" />,
}

const statusTone: Record<PlaythroughStatus, BadgeTone> = {
  Planning: 'neutral',
  Active: 'aqua',
  Completed: 'success',
}

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  name: string
}

function pct(done: number, total: number): number {
  return total > 0 ? Math.round((done / total) * 100) : 0
}

export function GamesPage() {
  const { t } = useTranslation('games')
  const { session } = useSession()
  const currentUserId = session?.userId ?? null
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [toast, setToast] = useState<ToastState | null>(null)

  const {
    state,
    dialog,
    listQuery,
    setFilters,
    setSortField,
    toggleSortDirection,
    setPage,
    setPageSize,
    clearFilters,
    openCreatePlaythrough,
    closeDialog,
  } = useGamesCollectionState(currentUserId)

  const games = useGames()
  const playthroughsQuery = usePlaythroughs(listQuery)

  const data = playthroughsQuery.data
  const playthroughs = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / state.pageSize))
  const activeCount = playthroughs.filter((p) => p.status === 'Active').length
  const hasFilters = activePlaythroughFilterCount(state) > 0

  const openProgress = (playthroughId: number) =>
    void navigate(`/games/playthroughs/${playthroughId}`)

  const invalidatePlaythroughs = (playthroughId?: number) => {
    void queryClient.invalidateQueries({ queryKey: gamesKeys.playthroughs() })
    if (playthroughId != null) {
      void queryClient.invalidateQueries({
        queryKey: gamesKeys.playthrough(playthroughId),
      })
    }
  }

  const handleSaved = (playthrough: Playthrough, mode: 'create' | 'edit') => {
    queryClient.setQueryData(gamesKeys.playthrough(playthrough.id), playthrough)
    invalidatePlaythroughs(playthrough.id)
    setToast({
      kind: mode === 'create' ? 'created' : 'updated',
      name: playthrough.name,
    })
    closeDialog()
  }

  const handleDeleted = (playthrough: Playthrough) => {
    invalidatePlaythroughs()
    setToast({ kind: 'deleted', name: playthrough.name })
    closeDialog()
  }

  useEffect(() => {
    if (data != null && state.page > totalPages) setPage(totalPages)
  }, [data, state.page, totalPages, setPage])

  if (playthroughsQuery.isError) {
    const error = playthroughsQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void playthroughsQuery.refetch()} />
    }
  }

  return (
    <main className="seg-games armali-aurora">
      <section className="seg-games__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
        <div className="seg-games__head-actions">
          <div className="seg-games__stats">
            <Badge tone="neutral">
              {t('stats.playthroughs', { count: totalCount })}
            </Badge>
            <Badge tone="aqua">{t('stats.active', { count: activeCount })}</Badge>
          </div>
          <Button iconLeft={<Plus size={16} />} onClick={openCreatePlaythrough}>
            {t('collection.newPlaythrough')}
          </Button>
        </div>
      </section>

      <GamesFilters
        state={state}
        games={games.data ?? []}
        onChange={setFilters}
        onClear={clearFilters}
      />

      <section className="seg-games__sortbar">
        <span className="seg-games__sort-label">{t('collection.sortLabel')}</span>
        <label className="seg-games__sort-field">
          <span className="seg-games__sr-only">{t('collection.sortLabel')}</span>
          <Select
            aria-label={t('collection.sortLabel')}
            value={state.sort}
            onChange={(event) =>
              setSortField(event.target.value as PlaythroughSortField)
            }
            options={sortFields.map((field) => ({
              value: field,
              label: t(`collection.sort.${field}`),
            }))}
          />
        </label>
        <button
          type="button"
          className="seg-games__sort-dir"
          onClick={toggleSortDirection}
          aria-label={t('collection.sortDirection.toggle')}
          title={t(`collection.sortDirection.${state.sortDirection}`)}
        >
          {state.sortDirection === 'asc' ? (
            <ArrowUp size={16} aria-hidden="true" />
          ) : (
            <ArrowDown size={16} aria-hidden="true" />
          )}
        </button>
      </section>

      {playthroughsQuery.isPending ? (
        <div className="seg-games__loading">
          <Spinner label={t('collection.states.loading')} />
        </div>
      ) : playthroughsQuery.isError ? (
        <p className="seg-games__error" role="alert">
          {t('collection.states.loadError')}
        </p>
      ) : playthroughs.length === 0 ? (
        <p className="seg-games__empty">
          {hasFilters
            ? t('collection.states.emptyFiltered')
            : t('collection.states.empty')}
        </p>
      ) : (
        <section
          className="seg-games__gallery"
          aria-busy={playthroughsQuery.isFetching && !playthroughsQuery.isPending}
        >
          {playthroughs.map((playthrough) => (
            <PlaythroughCard
              key={playthrough.id}
              playthrough={playthrough}
              onOpen={() => openProgress(playthrough.id)}
            />
          ))}
        </section>
      )}

      <Pager
        page={state.page}
        pageSize={state.pageSize}
        totalPages={totalPages}
        fetching={playthroughsQuery.isFetching}
        onPage={setPage}
        onPageSize={setPageSize}
      />

      {dialog.mode !== 'closed' && (
        <PlaythroughDialog
          mode={dialog.mode === 'createPlaythrough' ? 'create' : 'edit'}
          playthroughId={
            dialog.mode === 'editPlaythrough' ? dialog.playthroughId : undefined
          }
          currentUserId={currentUserId}
          onClose={closeDialog}
          onSaved={handleSaved}
          onDeleted={handleDeleted}
          onOpenProgress={openProgress}
        />
      )}

      {toast != null && (
        <div className="seg-games__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            closeLabel={t('editor.actions.cancel')}
            onClose={() => setToast(null)}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface GamesFiltersProps {
  state: PlaythroughListState
  games: ReadonlyArray<{ id: number; name: string }>
  onChange: (patch: PlaythroughFilterPatch) => void
  onClear: () => void
}

function GamesFilters({ state, games, onChange, onClear }: GamesFiltersProps) {
  const { t } = useTranslation('games')
  const [searchText, setSearchText] = useState(state.search)
  const [lastExternalSearch, setLastExternalSearch] = useState(state.search)
  const [tagText, setTagText] = useState(state.tag)
  const [lastExternalTag, setLastExternalTag] = useState(state.tag)

  if (state.search !== lastExternalSearch) {
    setLastExternalSearch(state.search)
    setSearchText(state.search)
  }
  if (state.tag !== lastExternalTag) {
    setLastExternalTag(state.tag)
    setTagText(state.tag)
  }

  const gameName = games.find((game) => game.id === state.game)?.name ?? ''
  const chips = buildChips(state, gameName, t)

  return (
    <div className="seg-games__filters">
      <Input
        className="seg-games__search"
        iconLeft={<Search size={16} />}
        label={t('collection.filters.searchLabel')}
        placeholder={t('collection.filters.searchPlaceholder')}
        value={searchText}
        onChange={(event) => {
          setSearchText(event.target.value)
          onChange({ search: event.target.value })
        }}
      />
      <label className="seg-games__field">
        <span>{t('collection.filters.game')}</span>
        <Select
          value={state.game == null ? '' : String(state.game)}
          onChange={(event) =>
            onChange({
              game: event.target.value === '' ? null : Number(event.target.value),
            })
          }
          options={[
            { value: '', label: t('collection.filters.allGames') },
            ...games.map((game) => ({ value: String(game.id), label: game.name })),
          ]}
        />
      </label>
      <label className="seg-games__field">
        <span>{t('collection.filters.platform')}</span>
        <Select
          value={state.platform}
          onChange={(event) =>
            onChange({ platform: event.target.value as GamePlatform | '' })
          }
          options={[
            { value: '', label: t('collection.filters.allPlatforms') },
            ...gamePlatforms.map((platform) => ({
              value: platform,
              label: t(`platform.${platform}`),
            })),
          ]}
        />
      </label>
      <label className="seg-games__field">
        <span>{t('collection.filters.status')}</span>
        <Select
          value={state.status}
          onChange={(event) =>
            onChange({ status: event.target.value as PlaythroughStatus | '' })
          }
          options={[
            { value: '', label: t('collection.filters.anyStatus') },
            ...playthroughStatuses.map((status) => ({
              value: status,
              label: t(`status.${status}`),
            })),
          ]}
        />
      </label>
      <Input
        className="seg-games__tag"
        iconLeft={<Tag size={16} />}
        label={t('collection.filters.tag')}
        placeholder={t('collection.filters.tagPlaceholder')}
        value={tagText}
        onChange={(event) => {
          setTagText(event.target.value)
          onChange({ tag: event.target.value })
        }}
      />
      <label className="seg-games__field">
        <span>{t('collection.filters.visibility')}</span>
        <Select
          value={state.visibility}
          onChange={(event) =>
            onChange({ visibility: event.target.value as GamesVisibility | '' })
          }
          options={[
            { value: '', label: t('collection.filters.anyVisibility') },
            { value: 'Public', label: t('visibility.Public') },
            { value: 'Private', label: t('visibility.Private') },
          ]}
        />
      </label>
      <label className="seg-games__field">
        <span>{t('collection.filters.mine')}</span>
        <Select
          value={state.mine ? 'true' : ''}
          onChange={(event) => onChange({ mine: event.target.value === 'true' })}
          options={[
            { value: '', label: t('collection.filters.everyone') },
            { value: 'true', label: t('collection.filters.onlyMine') },
          ]}
        />
      </label>

      {chips.length > 0 && (
        <div
          className="seg-games__chips"
          role="group"
          aria-label={t('collection.filters.activeLabel')}
        >
          {chips.map((chip) => (
            <button
              key={chip.key}
              type="button"
              className="seg-games__chip"
              onClick={() => onChange(chip.clear)}
              aria-label={t('collection.filters.remove', { label: chip.label })}
            >
              {chip.label}
            </button>
          ))}
          <button type="button" className="seg-games__chip-clear" onClick={onClear}>
            {t('collection.filters.clearAll')}
          </button>
        </div>
      )}
    </div>
  )
}

interface FilterChip {
  key: string
  label: string
  clear: PlaythroughFilterPatch
}

function buildChips(
  state: PlaythroughListState,
  gameName: string,
  t: (key: string, options?: Record<string, unknown>) => string,
): FilterChip[] {
  const chips: FilterChip[] = []
  if (state.search.trim() !== '') {
    chips.push({
      key: 'search',
      label: t('collection.filters.chip.search', { value: state.search.trim() }),
      clear: { search: '' },
    })
  }
  if (state.game != null) {
    chips.push({
      key: 'game',
      label: t('collection.filters.chip.game', {
        value: gameName || String(state.game),
      }),
      clear: { game: null },
    })
  }
  if (state.platform !== '') {
    chips.push({
      key: 'platform',
      label: t('collection.filters.chip.platform', {
        value: t(`platform.${state.platform}`),
      }),
      clear: { platform: '' },
    })
  }
  if (state.status !== '') {
    chips.push({
      key: 'status',
      label: t('collection.filters.chip.status', {
        value: t(`status.${state.status}`),
      }),
      clear: { status: '' },
    })
  }
  if (state.tag.trim() !== '') {
    chips.push({
      key: 'tag',
      label: t('collection.filters.chip.tag', { value: state.tag.trim() }),
      clear: { tag: '' },
    })
  }
  if (state.visibility !== '') {
    chips.push({
      key: 'visibility',
      label: t('collection.filters.chip.visibility', {
        value: t(`visibility.${state.visibility}`),
      }),
      clear: { visibility: '' },
    })
  }
  if (state.mine) {
    chips.push({
      key: 'mine',
      label: t('collection.filters.chip.mine'),
      clear: { mine: false },
    })
  }
  return chips
}

interface PlaythroughCardProps {
  playthrough: PlaythroughSummary
  onOpen: () => void
}

function PlaythroughCard({ playthrough, onOpen }: PlaythroughCardProps) {
  const { t } = useTranslation('games')
  const done = playthrough.progress.completedGoals
  const total = playthrough.progress.totalGoals
  const percent = pct(done, total)
  const empty = total === 0
  const shownTags = playthrough.tags.slice(0, 3)
  const extraTags = playthrough.tags.length - shownTags.length

  return (
    <button
      type="button"
      className="seg-games-card"
      onClick={onOpen}
      aria-label={t('collection.open', { name: playthrough.name })}
    >
      <div className="seg-games-card__top">
        <span className="seg-games-card__cover">
          {platformIcon[playthrough.platform]}
        </span>
        <div className="seg-games-card__head">
          <div className="seg-games-card__name">{playthrough.name}</div>
          <div className="seg-games-card__game">
            <Gamepad2 size={13} aria-hidden="true" />
            {playthrough.gameName}
          </div>
        </div>
        <VisibilityMark visibility={playthrough.visibility} />
      </div>

      <div className="seg-games-card__meta">
        <Badge
          tone={statusTone[playthrough.status]}
          dot
          pulse={playthrough.status === 'Active'}
        >
          {t(`status.${playthrough.status}`)}
        </Badge>
        <span className="seg-games-plat">{t(`platform.${playthrough.platform}`)}</span>
      </div>

      <div className={'seg-games-progbar' + (empty ? ' is-empty' : '')}>
        <div className="seg-games-progbar__top">
          <span className="seg-games-progbar__count">
            {empty ? t('progress.none') : t('progress.count', { done, total })}
          </span>
          <span className="seg-games-progbar__pct">
            {empty ? '—' : t('progress.percent', { percent })}
          </span>
        </div>
        <div className="seg-games-progbar__track">
          <div
            className="seg-games-progbar__fill"
            style={{ width: `${empty ? 0 : percent}%` }}
          />
        </div>
      </div>

      {shownTags.length > 0 && (
        <div className="seg-games-tags">
          {shownTags.map((tag) => (
            <span key={tag} className="seg-games-tag">
              <Tag size={11} aria-hidden="true" />
              {tag}
            </span>
          ))}
          {extraTags > 0 && (
            <span className="seg-games-tag">
              {t('collection.moreTags', { count: extraTags })}
            </span>
          )}
        </div>
      )}

      <div className="seg-games-card__foot">
        <span className="seg-games-card__date">
          <Calendar size={13} aria-hidden="true" />
          {t('collection.started', {
            date: formatStart(playthrough.startMonth, playthrough.startYear, t),
          })}
        </span>
        <span className="seg-games-card__open">
          {t('collection.openProgress')}
          <ArrowRight size={13} aria-hidden="true" />
        </span>
      </div>
    </button>
  )
}

function VisibilityMark({ visibility }: { visibility: GamesVisibility }) {
  const { t } = useTranslation('games')
  const isPrivate = visibility === 'Private'
  return (
    <span className={'seg-games-vis' + (isPrivate ? ' is-private' : '')}>
      {isPrivate ? (
        <Lock size={13} aria-hidden="true" />
      ) : (
        <Globe size={13} aria-hidden="true" />
      )}
      {t(`visibility.${visibility}`)}
    </span>
  )
}

function formatStart(month: number, year: number, t: (key: string) => string): string {
  const safeMonth = Math.min(Math.max(month, 1), 12)
  return `${t(`editor.months.${safeMonth}`)} ${year}`
}

interface PagerProps {
  page: number
  pageSize: GamesPageSize
  totalPages: number
  fetching: boolean
  onPage: (page: number) => void
  onPageSize: (pageSize: GamesPageSize) => void
}

function Pager({
  page,
  pageSize,
  totalPages,
  fetching,
  onPage,
  onPageSize,
}: PagerProps) {
  const { t } = useTranslation('games')
  return (
    <nav
      className="seg-games__pager"
      aria-label={t('pagination.status', { page, pages: totalPages })}
    >
      <label className="seg-games__rows">
        <span>{t('pagination.rowsPerPage')}</span>
        <Select
          value={String(pageSize)}
          onChange={(event) => onPageSize(Number(event.target.value) as GamesPageSize)}
          options={gamesPageSizes.map((size) => ({
            value: String(size),
            label: String(size),
          }))}
        />
      </label>
      <div className="seg-games__pager-nav">
        <Button
          variant="ghost"
          size="sm"
          iconLeft={<ChevronLeft size={16} />}
          disabled={page <= 1 || fetching}
          onClick={() => onPage(Math.max(1, page - 1))}
        >
          {t('pagination.previous')}
        </Button>
        <span className="seg-games__page" aria-live="polite">
          {t('pagination.status', { page, pages: totalPages })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          iconRight={<ChevronRight size={16} />}
          disabled={page >= totalPages || fetching}
          onClick={() => onPage(Math.min(totalPages, page + 1))}
        >
          {t('pagination.next')}
        </Button>
      </div>
    </nav>
  )
}
