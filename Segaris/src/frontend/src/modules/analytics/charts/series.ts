/**
 * Normalized chart point shapes the Analytics primitives consume. Tab panels
 * map the backend chart DTOs (which use verbose `selectedYearAmountEur` style
 * fields) onto these compact `current` / `previous` points so every chart
 * primitive shares one vocabulary.
 */

/** A single category or month with its selected-year and previous-year value. */
export interface AnalyticsComparisonPoint {
  label: string
  current: number
  previous: number
}

/** A ranked point that also carries its share of the relevant total. */
export interface AnalyticsTopPoint extends AnalyticsComparisonPoint {
  currentPercent: number
  previousPercent: number
}
