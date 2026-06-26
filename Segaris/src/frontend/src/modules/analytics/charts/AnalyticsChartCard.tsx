import { ChartColumnBig, Info, Table2 } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { AnalyticsChartEmpty } from './AnalyticsChartEmpty'
import { AnalyticsChartSummary } from './AnalyticsChartSummary'
import { AnalyticsDataTable, type AnalyticsDataTableModel } from './AnalyticsDataTable'

export type AnalyticsChartHeight = 'md' | 'lg' | 'xl'

interface AnalyticsChartCardProps {
  title: string
  /** Uppercase category line above the title. */
  eyebrow?: string
  icon?: ReactNode
  sub?: string
  legend?: ReactNode
  /** Accessible prose summary; also the `aria-label` of the chart body. */
  summary: string
  /** Table equivalent; when present, a toggle swaps the chart for the table. */
  table?: AnalyticsDataTableModel
  note?: string
  height?: AnalyticsChartHeight
  /** Renders the empty placeholder instead of the chart children. */
  isEmpty?: boolean
  className?: string
  children: ReactNode
}

const heightClass: Record<AnalyticsChartHeight, string> = {
  md: 'an-chart--md',
  lg: 'an-chart--lg',
  xl: 'an-chart--xl',
}

/**
 * Frosted shell around every Analytics chart. Owns the header (eyebrow, title,
 * legend), the chart/table toggle, an optional footnote, and the screen-reader
 * summary, so individual charts only supply their plotted body.
 */
export function AnalyticsChartCard({
  title,
  eyebrow,
  icon,
  sub,
  legend,
  summary,
  table,
  note,
  height = 'md',
  isEmpty = false,
  className,
  children,
}: AnalyticsChartCardProps) {
  const { t } = useTranslation('analytics')
  const [showTable, setShowTable] = useState(false)
  const canToggleTable = table != null && !isEmpty

  return (
    <section className={['an-card', className].filter(Boolean).join(' ')}>
      <div className="an-card__head">
        <div className="an-card__titles">
          {eyebrow != null && (
            <div className="an-card__eyebrow">
              {icon}
              {eyebrow}
            </div>
          )}
          <h3 className="an-card__title">{title}</h3>
          {sub != null && <div className="an-card__sub">{sub}</div>}
        </div>
        <div className="an-card__tools">
          {legend}
          {canToggleTable && (
            <button
              type="button"
              className={['an-tbtn', showTable ? 'is-active' : '']
                .filter(Boolean)
                .join(' ')}
              aria-pressed={showTable}
              title={showTable ? t('chart.showChart') : t('chart.showTable')}
              onClick={() => setShowTable((value) => !value)}
            >
              {showTable ? (
                <ChartColumnBig size={16} aria-hidden="true" />
              ) : (
                <Table2 size={16} aria-hidden="true" />
              )}
            </button>
          )}
        </div>
      </div>
      {showTable && table != null ? (
        <AnalyticsDataTable {...table} />
      ) : (
        <div
          className={`an-chart ${heightClass[height]}`}
          role="img"
          aria-label={summary}
        >
          {isEmpty ? <AnalyticsChartEmpty /> : children}
        </div>
      )}
      {note != null && !showTable && (
        <div className="an-card__note">
          <Info size={13} aria-hidden="true" />
          {note}
        </div>
      )}
      <AnalyticsChartSummary>{summary}</AnalyticsChartSummary>
    </section>
  )
}
