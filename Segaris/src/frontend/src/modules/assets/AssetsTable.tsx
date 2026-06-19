import { ArrowDown, ArrowUp, ChevronsUpDown, ImageOff } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import type {
  AssetSortField,
  AssetStatus,
  AssetSummary,
  AssetVisibility,
} from '@/app/api/assets'
import { formatDate } from '@/app/i18n/formatters'
import { Badge, type BadgeTone } from '@/components/ui'

import type { AssetsState } from './assetsState'

interface Column {
  field: AssetSortField
  key: keyof typeof columnLabels
}

const columnLabels = {
  name: 'assets.columns.name',
  code: 'assets.columns.code',
  category: 'assets.columns.category',
  location: 'assets.columns.location',
  status: 'assets.columns.status',
  expectedEndOfLife: 'assets.columns.expectedEndOfLife',
  visibility: 'assets.columns.visibility',
} as const

const columns: Column[] = [
  { field: 'name', key: 'name' },
  { field: 'code', key: 'code' },
  { field: 'category', key: 'category' },
  { field: 'location', key: 'location' },
  { field: 'status', key: 'status' },
  { field: 'expectedEndOfLife', key: 'expectedEndOfLife' },
  { field: 'visibility', key: 'visibility' },
]

const statusTone: Record<AssetStatus, BadgeTone> = {
  Active: 'success',
  Stored: 'gold',
  Retired: 'neutral',
}

const visibilityTone: Record<AssetVisibility, BadgeTone> = {
  Public: 'azure',
  Private: 'neutral',
}

interface AssetsTableProps {
  assets: AssetSummary[]
  state: AssetsState
  language: string
  onSort: (field: AssetSortField) => void
  onOpen: (assetId: number) => void
  busy: boolean
}

export function AssetsTable({
  assets,
  state,
  language,
  onSort,
  onOpen,
  busy,
}: AssetsTableProps) {
  const { t } = useTranslation('assets')

  return (
    <div className="seg-assets__table-wrap" aria-busy={busy}>
      <table className="seg-assets__table">
        <thead>
          <tr>
            <th className="seg-assets__thumb-col">
              <span className="seg-assets__sr">{t('assets.columns.thumbnail')}</span>
            </th>
            {columns.map((column) => {
              const active = state.sort === column.field
              const direction = active ? state.sortDirection : undefined
              const ariaSort = !active
                ? 'none'
                : state.sortDirection === 'asc'
                  ? 'ascending'
                  : 'descending'
              return (
                <th key={column.field} aria-sort={ariaSort}>
                  <button
                    type="button"
                    className={'seg-assets__sort' + (active ? ' is-active' : '')}
                    onClick={() => onSort(column.field)}
                    aria-label={t('assets.sort.label', {
                      column: t(columnLabels[column.key]),
                    })}
                  >
                    <span>{t(columnLabels[column.key])}</span>
                    {direction === 'asc' ? (
                      <ArrowUp size={14} aria-hidden="true" />
                    ) : direction === 'desc' ? (
                      <ArrowDown size={14} aria-hidden="true" />
                    ) : (
                      <ChevronsUpDown size={14} aria-hidden="true" />
                    )}
                  </button>
                </th>
              )
            })}
          </tr>
        </thead>
        <tbody>
          {assets.map((asset) => (
            <tr
              key={asset.id}
              className="seg-assets__row"
              onClick={() => onOpen(asset.id)}
            >
              <td className="seg-assets__thumb-col">
                {asset.thumbnail.url != null ? (
                  <img
                    className="seg-assets__thumb"
                    src={asset.thumbnail.url}
                    alt=""
                    loading="lazy"
                  />
                ) : (
                  <span className="seg-assets__thumb seg-assets__thumb--empty">
                    <ImageOff size={18} aria-hidden="true" />
                  </span>
                )}
              </td>
              <td className="seg-assets__name">
                <button
                  type="button"
                  className="seg-assets__row-open"
                  onClick={(event) => {
                    event.stopPropagation()
                    onOpen(asset.id)
                  }}
                  aria-label={t('assets.openRow', { name: asset.name })}
                >
                  {asset.name}
                </button>
              </td>
              <td>{asset.code ?? t('common.none')}</td>
              <td>{asset.categoryName}</td>
              <td>{asset.locationName}</td>
              <td>
                <Badge tone={statusTone[asset.status]} dot>
                  {t(`assets.status.${asset.status}`)}
                </Badge>
              </td>
              <td>
                {asset.expectedEndOfLifeDate == null
                  ? t('common.none')
                  : formatDate(asset.expectedEndOfLifeDate, language)}
              </td>
              <td>
                <Badge tone={visibilityTone[asset.visibility]}>
                  {t(`assets.visibility.${asset.visibility}`)}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
