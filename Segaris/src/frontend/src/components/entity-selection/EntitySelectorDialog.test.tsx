import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'

import {
  EntitySelectorDialog,
  type EntitySelectorColumn,
  type EntitySelectorFilter,
  type EntitySelectorLabels,
  type EntitySelectorState,
} from './EntitySelectorDialog'

interface Row {
  id: string
  name: string
  code: string
  category: string
  status: string
}

const ROWS: Row[] = [
  { id: '1', name: 'Alpha', code: 'A-1', category: 'Furniture', status: 'Active' },
  { id: '2', name: 'Bravo', code: 'B-2', category: 'Kitchen', status: 'Active' },
  { id: '3', name: 'Charlie', code: 'C-3', category: 'Furniture', status: 'Stored' },
  { id: '4', name: 'Delta', code: 'D-4', category: 'Kitchen', status: 'Active' },
  { id: '5', name: 'Echo', code: 'E-5', category: 'Furniture', status: 'Active' },
  { id: '6', name: 'Foxtrot', code: 'F-6', category: 'Kitchen', status: 'Stored' },
  { id: '7', name: 'Golf', code: 'G-7', category: 'Furniture', status: 'Active' },
]

const columns: ReadonlyArray<EntitySelectorColumn<Row>> = [
  { id: 'name', header: 'Name', sortField: 'name', render: (row) => row.name },
  { id: 'code', header: 'Code', render: (row) => row.code },
  {
    id: 'category',
    header: 'Category',
    sortField: 'category',
    render: (row) => row.category,
  },
  { id: 'status', header: 'Status', render: (row) => row.status },
]

const filters: ReadonlyArray<EntitySelectorFilter> = [
  {
    id: 'category',
    label: 'Category',
    options: [
      { value: 'all', label: 'All categories' },
      { value: 'Furniture', label: 'Furniture' },
      { value: 'Kitchen', label: 'Kitchen' },
    ],
  },
  {
    id: 'status',
    label: 'Status',
    options: [
      { value: 'all', label: 'All statuses' },
      { value: 'Active', label: 'Active' },
      { value: 'Stored', label: 'Stored' },
    ],
  },
]

const labels: EntitySelectorLabels = {
  title: 'Select an item',
  eyebrow: 'Link',
  description: 'Pick one.',
  searchPlaceholder: 'Search',
  searchLabel: 'Search items',
  resultCount: (count) => `${count} match`,
  pageInfo: (start, end, total) => `${start}-${end} of ${total}`,
  clearAll: 'Clear all',
  removeFilter: (label) => `Remove ${label}`,
  selectAction: 'Select',
  currentTag: 'Linked',
  cancel: 'Cancel',
  close: 'Close',
  loading: 'Loading…',
  refetching: 'Refreshing',
  error: 'Could not load.',
  retry: 'Retry',
  empty: 'Nothing here yet.',
  filteredEmpty: 'No matches.',
  clearFilters: 'Clear filters',
  previousPage: 'Previous page',
  nextPage: 'Next page',
}

interface HarnessOptions {
  pageSize?: number
  currentId?: string | null
  force?: { isLoading?: boolean; isError?: boolean }
  onSelect?: (row: Row) => void
  onClose?: () => void
  refetch?: () => void
}

function queryRows(state: EntitySelectorState): Row[] {
  const q = state.search.trim().toLowerCase()
  let rows = ROWS.filter((row) => {
    if (q && !`${row.name} ${row.code}`.toLowerCase().includes(q)) return false
    if (state.filters.category !== 'all' && row.category !== state.filters.category)
      return false
    if (state.filters.status !== 'all' && row.status !== state.filters.status)
      return false
    return true
  })
  if (state.sort != null) {
    const dir = state.sort.direction === 'asc' ? 1 : -1
    const field = state.sort.field as keyof Row
    rows = [...rows].sort((a, b) =>
      a[field] < b[field] ? -dir : a[field] > b[field] ? dir : 0,
    )
  }
  return rows
}

function renderSelector(options: HarnessOptions = {}) {
  const onSelect = options.onSelect ?? vi.fn()
  const onClose = options.onClose ?? vi.fn()
  const refetch = options.refetch ?? vi.fn()

  const useEntities = (state: EntitySelectorState) => {
    const all = queryRows(state)
    const start = (state.page - 1) * state.pageSize
    return {
      items:
        options.force?.isLoading || options.force?.isError
          ? []
          : all.slice(start, start + state.pageSize),
      total: options.force?.isLoading || options.force?.isError ? 0 : all.length,
      isLoading: options.force?.isLoading ?? false,
      isFetching: false,
      isError: options.force?.isError ?? false,
      refetch,
    }
  }

  render(
    <EntitySelectorDialog<Row>
      useEntities={useEntities}
      columns={columns}
      filters={filters}
      labels={labels}
      rowId={(row) => row.id}
      currentId={options.currentId ?? null}
      onSelect={onSelect}
      onClose={onClose}
      defaultSort={{ field: 'name', direction: 'asc' }}
      pageSize={options.pageSize ?? 10}
      searchDebounceMs={0}
    />,
  )

  return { onSelect, onClose, refetch }
}

function dataRowNames(): string[] {
  return screen
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('cell')[0].textContent ?? '')
}

describe('EntitySelectorDialog', () => {
  it('renders the heading, result count, and rows', () => {
    renderSelector()
    expect(screen.getByRole('dialog', { name: /Select an item/ })).toBeInTheDocument()
    expect(screen.getByText('7 match')).toBeInTheDocument()
    expect(dataRowNames()).toEqual([
      'Alpha',
      'Bravo',
      'Charlie',
      'Delta',
      'Echo',
      'Foxtrot',
      'Golf',
    ])
  })

  it('paginates with a numbered footer', async () => {
    const user = userEvent.setup()
    renderSelector({ pageSize: 3 })

    expect(screen.getByText('1-3 of 7')).toBeInTheDocument()
    expect(dataRowNames()).toEqual(['Alpha', 'Bravo', 'Charlie'])

    await user.click(screen.getByRole('button', { name: '2' }))
    expect(screen.getByText('4-6 of 7')).toBeInTheDocument()
    expect(dataRowNames()).toEqual(['Delta', 'Echo', 'Foxtrot'])
  })

  it('debounces search into the query and shows a chip', async () => {
    const user = userEvent.setup()
    renderSelector()

    await user.type(screen.getByRole('searchbox', { name: 'Search items' }), 'Fox')

    await waitFor(() => expect(dataRowNames()).toEqual(['Foxtrot']))
    expect(screen.getByText('1 match')).toBeInTheDocument()
    expect(screen.getByText('“Fox”')).toBeInTheDocument()
  })

  it('filters by a caller-provided select and clears all', async () => {
    const user = userEvent.setup()
    renderSelector()

    await user.selectOptions(
      screen.getByRole('combobox', { name: 'Category' }),
      'Kitchen',
    )
    expect(dataRowNames()).toEqual(['Bravo', 'Delta', 'Foxtrot'])
    expect(screen.getByRole('button', { name: 'Remove Kitchen' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Clear all' }))
    expect(dataRowNames()).toHaveLength(7)
  })

  it('sorts when a sortable header is toggled', async () => {
    const user = userEvent.setup()
    renderSelector()

    await user.click(screen.getByRole('button', { name: 'Name' }))
    expect(dataRowNames()[0]).toBe('Golf')
  })

  it('marks the current entity instead of offering Select', () => {
    renderSelector({ currentId: '2' })
    expect(screen.getByText('Linked')).toBeInTheDocument()
    // One Select per row except the current one.
    expect(screen.getAllByRole('button', { name: 'Select' })).toHaveLength(6)
  })

  it('selects a row and cancels', async () => {
    const user = userEvent.setup()
    const { onSelect, onClose } = renderSelector()

    const golfRow = screen.getByText('Golf').closest('[role="row"]') as HTMLElement
    await user.click(within(golfRow).getByRole('button', { name: 'Select' }))
    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ id: '7' }))

    await user.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('shows the loading state', () => {
    renderSelector({ force: { isLoading: true } })
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('shows the error state and retries', async () => {
    const user = userEvent.setup()
    const { refetch } = renderSelector({ force: { isError: true } })
    expect(screen.getByText('Could not load.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Retry' }))
    expect(refetch).toHaveBeenCalledTimes(1)
  })

  it('shows a filtered-empty state with a clear action', async () => {
    const user = userEvent.setup()
    renderSelector()

    await user.type(screen.getByRole('searchbox', { name: 'Search items' }), 'zzz')
    await waitFor(() => expect(screen.getByText('No matches.')).toBeInTheDocument())
    await user.click(screen.getByRole('button', { name: 'Clear filters' }))
    expect(dataRowNames()).toHaveLength(7)
  })
})
