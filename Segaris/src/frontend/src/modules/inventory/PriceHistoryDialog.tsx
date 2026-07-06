import { useQuery } from '@tanstack/react-query'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

import { inventoryApi, type InventoryItemSummary } from '@/app/api/inventory'
import { formatCurrency, formatDate, formatNumber } from '@/app/i18n/formatters'
import { Button, Dialog, Spinner } from '@/components/ui'

import { inventoryKeys } from './queries'

interface PriceHistoryDialogProps {
  item: InventoryItemSummary
  language: string
  onClose: () => void
}

export function PriceHistoryDialog({
  item,
  language,
  onClose,
}: PriceHistoryDialogProps) {
  const { t } = useTranslation('inventory')
  const historyQuery = useQuery({
    queryKey: inventoryKeys.itemPriceHistory(item.id),
    queryFn: ({ signal }) => inventoryApi.itemPriceHistory(item.id, signal),
  })
  const history = historyQuery.data
  const entries = history?.entries ?? []
  const chartEntries = useMemo(
    () =>
      [...entries].sort((left, right) => left.orderDate.localeCompare(right.orderDate)),
    [entries],
  )

  return (
    <Dialog
      title={t('items.priceHistory.title', { name: item.name })}
      description={t('items.priceHistory.description', {
        months: 12,
        count: history?.minimumRecentOrderCount ?? 24,
      })}
      width={860}
      scrollable
      onClose={onClose}
      closeLabel={t('editor.actions.cancel')}
      footer={
        <Button variant="ghost" onClick={onClose}>
          {t('items.priceHistory.close')}
        </Button>
      }
    >
      {historyQuery.isPending ? (
        <div className="seg-inv__history-loading">
          <Spinner />
        </div>
      ) : historyQuery.isError ? (
        <p className="seg-inv__error" role="alert">
          {t('items.priceHistory.loadError')}
        </p>
      ) : entries.length === 0 ? (
        <p className="seg-inv__empty">{t('items.priceHistory.empty')}</p>
      ) : (
        <div className="seg-inv__history">
          <PriceHistoryChart entries={chartEntries} language={language} />
          <div className="seg-inv__history-meta">
            {t('items.priceHistory.summary', {
              count: history?.returnedOrderCount ?? entries.length,
              cutoff: history?.cutoffDate
                ? formatDate(`${history.cutoffDate}T00:00:00Z`, language)
                : '',
            })}
          </div>
          <div className="seg-inv__history-table-wrap">
            <table className="seg-inv__history-table">
              <thead>
                <tr>
                  <th>{t('items.priceHistory.columns.date')}</th>
                  <th>{t('items.priceHistory.columns.supplier')}</th>
                  <th>{t('items.priceHistory.columns.status')}</th>
                  <th className="seg-inv__num">
                    {t('items.priceHistory.columns.quantity')}
                  </th>
                  <th className="seg-inv__num">
                    {t('items.priceHistory.columns.lineTotal')}
                  </th>
                  <th className="seg-inv__num">
                    {t('items.priceHistory.columns.unitPrice')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr key={entry.lineId}>
                    <td>{formatDate(`${entry.orderDate}T00:00:00Z`, language)}</td>
                    <td>{entry.supplierName}</td>
                    <td>{t(`orders.status.${entry.status}`)}</td>
                    <td className="seg-inv__num">
                      {formatNumber(entry.quantity, language)}
                    </td>
                    <td className="seg-inv__num">
                      {formatCurrency(entry.lineTotal, entry.currencyCode, language)}
                    </td>
                    <td className="seg-inv__num">
                      {formatCurrency(entry.unitPrice, entry.currencyCode, language)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </Dialog>
  )
}

interface ChartEntry {
  orderDate: string
  unitPrice: number
  currencyCode: string
}

function PriceHistoryChart({
  entries,
  language,
}: {
  entries: ChartEntry[]
  language: string
}) {
  const { t } = useTranslation('inventory')
  const width = 720
  const height = 240
  const padding = { top: 20, right: 24, bottom: 34, left: 62 }
  const prices = entries.map((entry) => entry.unitPrice)
  const min = Math.min(...prices)
  const max = Math.max(...prices)
  const span = max - min || 1
  const plotWidth = width - padding.left - padding.right
  const plotHeight = height - padding.top - padding.bottom
  const points = entries.map((entry, index) => {
    const x =
      padding.left +
      (entries.length === 1
        ? plotWidth / 2
        : (index / (entries.length - 1)) * plotWidth)
    const y = padding.top + plotHeight - ((entry.unitPrice - min) / span) * plotHeight
    return { ...entry, x, y }
  })
  const path = points
    .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`)
    .join(' ')
  const first = points[0]
  const last = points[points.length - 1]

  return (
    <figure className="seg-inv__history-chart">
      <figcaption className="seg-inv__sr">
        {t('items.priceHistory.chartLabel')}
      </figcaption>
      <svg
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label={t('items.priceHistory.chartLabel')}
      >
        <line
          x1={padding.left}
          y1={padding.top}
          x2={padding.left}
          y2={height - padding.bottom}
          className="seg-inv__history-axis"
        />
        <line
          x1={padding.left}
          y1={height - padding.bottom}
          x2={width - padding.right}
          y2={height - padding.bottom}
          className="seg-inv__history-axis"
        />
        <text x={padding.left - 8} y={padding.top + 5} textAnchor="end">
          {formatNumber(max, language)}
        </text>
        <text x={padding.left - 8} y={height - padding.bottom} textAnchor="end">
          {formatNumber(min, language)}
        </text>
        {first != null && (
          <text x={first.x} y={height - 10} textAnchor="middle">
            {formatDate(`${first.orderDate}T00:00:00Z`, language)}
          </text>
        )}
        {last != null && last !== first && (
          <text x={last.x} y={height - 10} textAnchor="middle">
            {formatDate(`${last.orderDate}T00:00:00Z`, language)}
          </text>
        )}
        <path d={path} className="seg-inv__history-line" />
        {points.map((point) => (
          <circle
            key={`${point.orderDate}-${point.unitPrice}-${point.currencyCode}`}
            cx={point.x}
            cy={point.y}
            r="4"
            className="seg-inv__history-point"
          >
            <title>
              {`${formatDate(`${point.orderDate}T00:00:00Z`, language)}: ${formatCurrency(
                point.unitPrice,
                point.currencyCode,
                language,
              )}`}
            </title>
          </circle>
        ))}
      </svg>
    </figure>
  )
}
