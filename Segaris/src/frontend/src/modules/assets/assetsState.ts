import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

import {
  assetPageSizes,
  type AssetListQuery,
  type AssetPageSize,
  type AssetSortDirection,
  type AssetSortField,
  type AssetStatus,
  type AssetVisibility,
} from '@/app/api/assets'

const sortFields: readonly AssetSortField[] = [
  'name',
  'code',
  'category',
  'location',
  'status',
  'expectedEndOfLife',
  'visibility',
]
const statuses: readonly AssetStatus[] = ['Active', 'Stored', 'Retired']
const visibilities: readonly AssetVisibility[] = ['Public', 'Private']

export const defaultSort: AssetSortField = 'name'
export const defaultSortDirection: AssetSortDirection = 'asc'
export const defaultPageSize: AssetPageSize = 25

export interface AssetsState {
  search: string
  category: number | null
  location: number | null
  status: AssetStatus | ''
  visibility: AssetVisibility | ''
  mine: boolean
  sort: AssetSortField
  sortDirection: AssetSortDirection
  page: number
  pageSize: AssetPageSize
}

export type AssetDialogState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; assetId: number }

function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | '' {
  return value != null && (allowed as readonly string[]).includes(value)
    ? (value as T)
    : ''
}

function intOrNull(value: string | null): number | null {
  if (value == null) return null
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

export function parseAssetsState(
  params: URLSearchParams,
  currentUserId: number | null,
): AssetsState {
  const sort = oneOf(params.get('sort'), sortFields)
  const direction = params.get('sortDirection')
  const pageSizeRaw = Number.parseInt(params.get('pageSize') ?? '', 10)
  const pageRaw = Number.parseInt(params.get('page') ?? '', 10)
  const creator = intOrNull(params.get('creator'))

  return {
    search: params.get('search') ?? '',
    category: intOrNull(params.get('category')),
    location: intOrNull(params.get('location')),
    status: oneOf(params.get('status'), statuses),
    visibility: oneOf(params.get('visibility'), visibilities),
    mine: creator != null && creator === currentUserId,
    sort: sort === '' ? defaultSort : sort,
    sortDirection:
      direction === 'asc' || direction === 'desc' ? direction : defaultSortDirection,
    page: Number.isFinite(pageRaw) && pageRaw > 0 ? pageRaw : 1,
    pageSize: (assetPageSizes as readonly number[]).includes(pageSizeRaw)
      ? (pageSizeRaw as AssetPageSize)
      : defaultPageSize,
  }
}

export function parseAssetDialogState(params: URLSearchParams): AssetDialogState {
  if (params.get('newAsset') === 'true') return { mode: 'create' }
  const assetId = intOrNull(params.get('assetId'))
  return assetId == null ? { mode: 'closed' } : { mode: 'edit', assetId }
}

export function toListQuery(
  state: AssetsState,
  currentUserId: number | null,
): AssetListQuery {
  return {
    search: state.search.trim() === '' ? null : state.search.trim(),
    category: state.category,
    location: state.location,
    status: state.status === '' ? null : state.status,
    visibility: state.visibility === '' ? null : state.visibility,
    creator: state.mine ? currentUserId : null,
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    sortDirection: state.sortDirection,
  }
}

export function useAssetsState(currentUserId: number | null) {
  const [searchParams, setSearchParams] = useSearchParams()
  const state = useMemo(
    () => parseAssetsState(searchParams, currentUserId),
    [searchParams, currentUserId],
  )
  const dialog = useMemo(() => parseAssetDialogState(searchParams), [searchParams])

  const setDialogParams = useCallback(
    (patch: { newAsset?: string | null; assetId?: string | null }) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (patch.newAsset == null) next.delete('newAsset')
        else next.set('newAsset', patch.newAsset)
        if (patch.assetId == null) next.delete('assetId')
        else next.set('assetId', patch.assetId)
        return next
      })
    },
    [setSearchParams],
  )

  return {
    state,
    dialog,
    listQuery: toListQuery(state, currentUserId),
    openCreateDialog: () => setDialogParams({ newAsset: 'true', assetId: null }),
    openEditDialog: (assetId: number) =>
      setDialogParams({ newAsset: null, assetId: String(assetId) }),
    closeDialog: () => setDialogParams({ newAsset: null, assetId: null }),
  }
}
