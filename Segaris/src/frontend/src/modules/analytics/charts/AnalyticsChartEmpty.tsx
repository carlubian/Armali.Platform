import { BarChart3 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

/** Placeholder shown inside a chart card when a chart has no data for the year. */
export function AnalyticsChartEmpty() {
  const { t } = useTranslation('analytics')
  return (
    <div className="an-chart__empty">
      <BarChart3 size={22} aria-hidden="true" />
      <span>{t('chart.empty')}</span>
    </div>
  )
}
