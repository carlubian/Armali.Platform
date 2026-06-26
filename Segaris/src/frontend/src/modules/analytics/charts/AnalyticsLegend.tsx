interface AnalyticsLegendProps {
  selectedYear: number
  previousYear: number
  color: string
  /** Optional override labels, e.g. `2026 net` / `2025 net` for the net chart. */
  currentLabel?: string
  previousLabel?: string
}

/**
 * Year-over-year legend: a solid swatch for the selected year and a faded,
 * outlined swatch for the previous year, matching how the charts draw them.
 */
export function AnalyticsLegend({
  selectedYear,
  previousYear,
  color,
  currentLabel,
  previousLabel,
}: AnalyticsLegendProps) {
  return (
    <div className="an-legend">
      <span className="an-legend__item">
        <span className="an-legend__swatch" style={{ background: color }} />
        {currentLabel ?? selectedYear}
      </span>
      <span className="an-legend__item">
        <span
          className="an-legend__swatch is-prev"
          style={{
            background: color,
            opacity: 0.3,
            boxShadow: `inset 0 0 0 1.5px ${color}`,
          }}
        />
        {previousLabel ?? previousYear}
      </span>
    </div>
  )
}
