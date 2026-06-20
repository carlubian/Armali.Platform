import { useEffect, useId, useMemo, useState } from 'react'
import type { CSSProperties, ReactNode } from 'react'

import { Button, Dialog, Input, Select, Spinner } from '@/components/ui'

import { pageItems } from './pagination'

import './EntitySelectorDialog.css'

export type SortDirection = 'asc' | 'desc'

/** Temporary, selector-local UI state owned by the dialog instance. */
export interface EntitySelectorState {
  /** Debounced search text. */
  search: string
  /** Filter id to selected value. */
  filters: Record<string, string>
  /** Active sort, or `null` for the adapter's default. */
  sort: { field: string; direction: SortDirection } | null
  page: number
  pageSize: number
}

/** What a caller-provided data hook returns for the current state. */
export interface EntityQueryResult<T> {
  items: readonly T[]
  /** Total matches across all pages, used for counts and pagination. */
  total: number
  /** Initial load with no data yet. */
  isLoading: boolean
  /** Background refetch while data is already shown. */
  isFetching: boolean
  isError: boolean
  refetch: () => void
}

export interface EntitySelectorColumn<T> {
  id: string
  header: ReactNode
  /** Sort field passed back in state; omit for a non-sortable column. */
  sortField?: string
  /** Default direction when the column is first sorted. Defaults to `asc`. */
  defaultSortDirection?: SortDirection
  /** CSS grid track size. Defaults to `minmax(0, 1fr)`. */
  width?: string
  align?: 'start' | 'end'
  render: (item: T) => ReactNode
}

export interface EntitySelectorFilterOption {
  value: string
  label: string
}

export interface EntitySelectorFilter {
  id: string
  /** Accessible label for the control. */
  label: string
  options: ReadonlyArray<EntitySelectorFilterOption>
  /**
   * Value treated as "no filter" for chips and clear-all. Defaults to the first
   * option's value (conventionally an "all" reset option).
   */
  emptyValue?: string
}

export interface EntitySelectorLabels {
  title: ReactNode
  eyebrow?: ReactNode
  description?: ReactNode
  searchPlaceholder: string
  searchLabel: string
  resultCount: (count: number) => ReactNode
  pageInfo: (start: number, end: number, total: number) => ReactNode
  clearAll: string
  removeFilter: (label: string) => string
  selectAction: string
  currentTag: ReactNode
  cancel: string
  close: string
  loading: ReactNode
  refetching: string
  error: ReactNode
  retry: string
  empty: ReactNode
  filteredEmpty: ReactNode
  clearFilters: string
  previousPage: string
  nextPage: string
}

export interface EntitySelectorDialogProps<T> {
  /** Runs the caller's query for the dialog's current state. */
  useEntities: (state: EntitySelectorState) => EntityQueryResult<T>
  columns: ReadonlyArray<EntitySelectorColumn<T>>
  filters?: ReadonlyArray<EntitySelectorFilter>
  labels: EntitySelectorLabels
  /** Stable identity for a row, compared against {@link currentId}. */
  rowId: (item: T) => string
  /** Identifier of the already-linked entity, marked as current. */
  currentId?: string | null
  onSelect: (item: T) => void
  onClose: () => void
  defaultSort?: { field: string; direction: SortDirection }
  pageSize?: number
  width?: number | string
  searchDebounceMs?: number
}

function emptyValueOf(filter: EntitySelectorFilter): string {
  return filter.emptyValue ?? filter.options[0]?.value ?? ''
}

function initialFilters(
  filters: ReadonlyArray<EntitySelectorFilter>,
): Record<string, string> {
  const result: Record<string, string> = {}
  for (const filter of filters) result[filter.id] = emptyValueOf(filter)
  return result
}

function SortChevron({ direction }: { direction: SortDirection | null }) {
  return (
    <span
      className={[
        'seg-sorth__chev',
        direction != null ? 'is-active' : '',
        direction === 'desc' ? 'is-desc' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <svg
        width="13"
        height="13"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.4"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        <path d="m6 15 6-6 6 6" />
      </svg>
    </span>
  )
}

function PagerArrow({ direction }: { direction: 'left' | 'right' }) {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={direction === 'left' ? 'm15 18-6-6 6-6' : 'm9 18 6-6-6-6'} />
    </svg>
  )
}

/**
 * A large, portal-backed selector dialog with a top filter bar, a sortable
 * table, active-filter chips, result counts, and numbered pagination. It owns
 * its temporary UI state (search, filters, sort, page) and delegates data,
 * columns, filters, labels, and selection to the caller, so a domain adapter
 * never copies this layout.
 *
 * Adapted from the entity-selector reference
 * (`docs/ui-design/segaris/screens-entity-selector.jsx`, `CapexSelector` top
 * variant).
 */
export function EntitySelectorDialog<T>({
  useEntities,
  columns,
  filters = [],
  labels,
  rowId,
  currentId,
  onSelect,
  onClose,
  defaultSort,
  pageSize: pageSizeProp = 8,
  width = 960,
  searchDebounceMs = 300,
}: EntitySelectorDialogProps<T>) {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [filterValues, setFilterValues] = useState<Record<string, string>>(() =>
    initialFilters(filters),
  )
  const [sort, setSort] = useState<{ field: string; direction: SortDirection } | null>(
    defaultSort ?? null,
  )
  const [page, setPage] = useState(1)
  const [pageSize] = useState(pageSizeProp)
  const headingId = useId()

  // Debounce the search text so server-backed adapters do not fire a request per
  // keystroke. Committing the search also returns to the first page. With a zero
  // delay this still flushes on the next tick.
  useEffect(() => {
    const handle = window.setTimeout(() => {
      setSearch(searchInput)
      setPage(1)
    }, searchDebounceMs)
    return () => window.clearTimeout(handle)
  }, [searchInput, searchDebounceMs])

  const state = useMemo<EntitySelectorState>(
    () => ({ search, filters: filterValues, sort, page, pageSize }),
    [search, filterValues, sort, page, pageSize],
  )

  const result = useEntities(state)
  const { items, total, isLoading, isFetching, isError } = result

  const pages = Math.max(1, Math.ceil(total / pageSize))
  // If results shrink under a stale high page, fold back during render (the
  // React-recommended alternative to a state-syncing effect); it converges
  // because `pages` is stable for the current results.
  if (!isLoading && page > pages) setPage(pages)

  // Narrowing the result set through a filter or sort returns to the first page.
  const setFilter = (id: string, value: string) => {
    setFilterValues((current) => ({ ...current, [id]: value }))
    setPage(1)
  }

  const onSort = (field: string, fallback: SortDirection) => {
    setSort((current) =>
      current?.field === field
        ? { field, direction: current.direction === 'asc' ? 'desc' : 'asc' }
        : { field, direction: fallback },
    )
    setPage(1)
  }

  const activeFilters = filters.flatMap((filter) => {
    const value = filterValues[filter.id]
    if (value === emptyValueOf(filter)) return []
    const option = filter.options.find((candidate) => candidate.value === value)
    return [{ id: filter.id, label: option?.label ?? value }]
  })
  if (search.trim().length > 0) {
    activeFilters.push({ id: '__search', label: `“${search.trim()}”` })
  }

  const clearFilter = (id: string) => {
    if (id === '__search') {
      setSearchInput('')
      setSearch('')
      setPage(1)
      return
    }
    const filter = filters.find((candidate) => candidate.id === id)
    if (filter != null) setFilter(id, emptyValueOf(filter))
  }

  const clearAll = () => {
    setSearchInput('')
    setSearch('')
    setFilterValues(initialFilters(filters))
    setPage(1)
  }

  const safePage = Math.min(page, pages)
  const start = total === 0 ? 0 : (safePage - 1) * pageSize
  const end = Math.min(start + pageSize, total)

  const gridTemplateColumns =
    columns.map((column) => column.width ?? 'minmax(0, 1fr)').join(' ') + ' auto'

  const hasActiveFilters = activeFilters.length > 0

  let body: ReactNode
  if (isLoading) {
    body = (
      <div className="seg-selstate" role="status">
        <Spinner />
        <p>{labels.loading}</p>
      </div>
    )
  } else if (isError) {
    body = (
      <div className="seg-selstate seg-selstate--error" role="alert">
        <p>{labels.error}</p>
        <Button variant="outline" size="sm" onClick={() => result.refetch()}>
          {labels.retry}
        </Button>
      </div>
    )
  } else if (total === 0) {
    body = (
      <div className="seg-selstate">
        <p>{hasActiveFilters ? labels.filteredEmpty : labels.empty}</p>
        {hasActiveFilters && (
          <Button variant="outline" size="sm" onClick={clearAll}>
            {labels.clearFilters}
          </Button>
        )}
      </div>
    )
  } else {
    body = (
      <div
        className="seg-seltable"
        role="table"
        style={{ '--seg-sel-cols': gridTemplateColumns } as CSSProperties}
      >
        <div className="seg-selhead" role="row">
          {columns.map((column) => {
            const sortable = column.sortField != null
            const active = sortable && sort?.field === column.sortField
            return (
              <span
                key={column.id}
                role="columnheader"
                className={
                  'seg-selth' + (column.align === 'end' ? ' seg-selth--end' : '')
                }
                aria-sort={
                  active
                    ? sort?.direction === 'asc'
                      ? 'ascending'
                      : 'descending'
                    : sortable
                      ? 'none'
                      : undefined
                }
              >
                {sortable ? (
                  <button
                    type="button"
                    className={'seg-sorth' + (active ? ' is-active' : '')}
                    onClick={() =>
                      onSort(
                        column.sortField as string,
                        column.defaultSortDirection ?? 'asc',
                      )
                    }
                  >
                    {column.header}
                    <SortChevron
                      direction={active ? (sort?.direction ?? null) : null}
                    />
                  </button>
                ) : (
                  column.header
                )}
              </span>
            )
          })}
          <span role="columnheader" className="seg-selth seg-selth--end" />
        </div>

        {items.map((item) => {
          const id = rowId(item)
          const current = currentId != null && id === currentId
          return (
            <div
              key={id}
              role="row"
              className={'seg-selrow' + (current ? ' is-current' : '')}
            >
              {columns.map((column) => (
                <span
                  key={column.id}
                  role="cell"
                  className={
                    'seg-selcell' + (column.align === 'end' ? ' seg-selcell--end' : '')
                  }
                >
                  {column.render(item)}
                </span>
              ))}
              <span role="cell" className="seg-selrow__act">
                {current ? (
                  <span className="seg-current-tag">{labels.currentTag}</span>
                ) : (
                  <Button variant="primary" size="sm" onClick={() => onSelect(item)}>
                    {labels.selectAction}
                  </Button>
                )}
              </span>
            </div>
          )
        })}
      </div>
    )
  }

  return (
    <Dialog
      scrollable
      width={width}
      className="seg-selector"
      onClose={onClose}
      closeLabel={labels.close}
      title={
        <span className="seg-selector__heading" id={headingId}>
          {labels.eyebrow != null && (
            <span className="armali-eyebrow">{labels.eyebrow}</span>
          )}
          {labels.title}
        </span>
      }
      description={labels.description}
      footer={
        <div className="seg-selector__foot">
          <span className="seg-pageinfo">
            {total === 0 ? null : labels.pageInfo(start + 1, end, total)}
          </span>
          {pages > 1 && (
            <nav className="seg-pager" aria-label={labels.title as string}>
              <button
                type="button"
                className="seg-pager__btn"
                disabled={safePage <= 1}
                onClick={() => setPage(safePage - 1)}
                aria-label={labels.previousPage}
              >
                <PagerArrow direction="left" />
              </button>
              {pageItems(safePage, pages).map((item, index) =>
                item === '…' ? (
                  <span key={`gap-${index}`} className="seg-pager__gap">
                    …
                  </span>
                ) : (
                  <button
                    key={item}
                    type="button"
                    className={
                      'seg-pager__btn' + (item === safePage ? ' is-active' : '')
                    }
                    aria-current={item === safePage ? 'page' : undefined}
                    onClick={() => setPage(item)}
                  >
                    {item}
                  </button>
                ),
              )}
              <button
                type="button"
                className="seg-pager__btn"
                disabled={safePage >= pages}
                onClick={() => setPage(safePage + 1)}
                aria-label={labels.nextPage}
              >
                <PagerArrow direction="right" />
              </button>
            </nav>
          )}
          <Button variant="ghost" onClick={onClose}>
            {labels.cancel}
          </Button>
        </div>
      }
    >
      <div className="seg-selector__filters">
        <Input
          className="seg-selector__search"
          type="search"
          aria-label={labels.searchLabel}
          placeholder={labels.searchPlaceholder}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        {filters.map((filter) => (
          <Select
            key={filter.id}
            aria-label={filter.label}
            value={filterValues[filter.id]}
            onChange={(event) => setFilter(filter.id, event.target.value)}
            options={filter.options}
          />
        ))}
      </div>

      <div className="seg-selector__strip">
        <span className="seg-selector__count">
          {labels.resultCount(total)}
          {isFetching && !isLoading && (
            <Spinner
              size={14}
              label={labels.refetching}
              className="seg-selector__busy"
            />
          )}
        </span>
        {hasActiveFilters && (
          <div className="seg-chips">
            {activeFilters.map((filter) => (
              <span key={filter.id} className="seg-chip">
                {filter.label}
                <button
                  type="button"
                  className="seg-chip__x"
                  onClick={() => clearFilter(filter.id)}
                  aria-label={labels.removeFilter(String(filter.label))}
                >
                  <svg
                    width="11"
                    height="11"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.6"
                    strokeLinecap="round"
                    aria-hidden="true"
                  >
                    <path d="M18 6 6 18M6 6l12 12" />
                  </svg>
                </button>
              </span>
            ))}
            <button type="button" className="seg-linkbtn" onClick={clearAll}>
              {labels.clearAll}
            </button>
          </div>
        )}
      </div>

      <div className="seg-selector__scroll">{body}</div>
    </Dialog>
  )
}
