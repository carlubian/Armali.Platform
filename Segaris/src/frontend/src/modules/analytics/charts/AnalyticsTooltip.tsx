import { useTranslation } from 'react-i18next'

import { formatDelta, formatEur, yearOverYear } from '../format'

interface TooltipPayloadEntry {
  dataKey?: string | number
  name?: string | number
  value?: number
  color?: string
}

interface AnalyticsTooltipProps {
  active?: boolean
  payload?: TooltipPayloadEntry[]
  label?: string | number
}

/**
 * Custom Recharts tooltip. Recharts' default tooltip is hidden in CSS so this
 * one owns the frosted styling, EUR formatting, and a year-over-year delta line
 * derived from the current/previous payload entries.
 */
export function AnalyticsTooltip({ active, payload, label }: AnalyticsTooltipProps) {
  const { t } = useTranslation('analytics')
  if (!active || payload == null || payload.length === 0) return null

  const current = payload.find((entry) => entry.dataKey === 'current')
  const previous = payload.find((entry) => entry.dataKey === 'previous')
  const delta =
    current?.value != null && previous?.value != null
      ? yearOverYear(current.value, previous.value)
      : null

  return (
    <div className="an-tip">
      {label != null && <div className="an-tip__label">{label}</div>}
      {payload.map((entry, index) => (
        <div className="an-tip__row" key={index}>
          <span
            className="an-tip__dot"
            style={{
              background: entry.color,
              opacity: entry.dataKey === 'previous' ? 0.5 : 1,
            }}
          />
          <span className="an-tip__k">{entry.name}</span>
          <span className="an-tip__v">{formatEur(entry.value ?? 0)}</span>
        </div>
      ))}
      {delta != null && (
        <div className="an-tip__delta">
          {t('chart.yoy', { delta: formatDelta(delta) })}
        </div>
      )}
    </div>
  )
}
