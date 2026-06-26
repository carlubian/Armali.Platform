import { useTranslation } from 'react-i18next'

import type { AnalyticsDataTableModel } from './charts'
import type { AnalyticsComparisonPoint, AnalyticsTopPoint } from './charts'
import { formatDelta, formatEur, formatPercent, yearOverYear } from './format'

export interface ChartNarrative {
  summary: string
  table: AnalyticsDataTableModel
}

function total(
  points: AnalyticsComparisonPoint[],
  key: 'current' | 'previous',
): number {
  return points.reduce((running, point) => running + point[key], 0)
}

/**
 * Builds the accessible summary and table equivalent shared by every chart, so
 * tooltips are never the only way to read a value. Localized through the
 * `analytics` namespace and the active language's EUR formatting.
 */
export function useChartNarrative() {
  const { t } = useTranslation('analytics')

  function deltaText(current: number, previous: number): string {
    const ratio = yearOverYear(current, previous)
    return ratio == null ? '—' : formatDelta(ratio)
  }

  function buildTable(
    dimension: string,
    points: AnalyticsComparisonPoint[],
    selectedYear: number,
    previousYear: number,
  ): AnalyticsDataTableModel {
    return {
      caption: t('chart.table.caption', {
        dimension,
        year: selectedYear,
        previousYear,
      }),
      columns: [
        dimension,
        String(selectedYear),
        String(previousYear),
        t('chart.table.yoy'),
      ],
      rows: points.map((point) => [
        point.label,
        formatEur(point.current),
        formatEur(point.previous),
        deltaText(point.current, point.previous),
      ]),
    }
  }

  function summaryText(
    title: string,
    points: AnalyticsComparisonPoint[],
    selectedYear: number,
    previousYear: number,
  ): string {
    if (points.length === 0) {
      return t('chart.summaryEmpty', { title, year: selectedYear })
    }
    const highlights = points
      .slice(0, 3)
      .map((point) => `${point.label} ${formatEur(point.current)}`)
      .join(', ')
    const currentTotal = total(points, 'current')
    const previousTotal = total(points, 'previous')
    return t('chart.summary', {
      title,
      year: selectedYear,
      highlights:
        points.length > 3 ? `${highlights}${t('chart.summaryMore')}` : highlights,
      current: formatEur(currentTotal),
      previous: formatEur(previousTotal),
      previousYear,
      delta: deltaText(currentTotal, previousTotal),
    })
  }

  function comparison(
    title: string,
    dimension: string,
    points: AnalyticsComparisonPoint[],
    selectedYear: number,
    previousYear: number,
  ): ChartNarrative {
    return {
      summary: summaryText(title, points, selectedYear, previousYear),
      table: buildTable(dimension, points, selectedYear, previousYear),
    }
  }

  /**
   * Top-N ranked narrative. Shares the comparison summary but its table adds a
   * "% of total" column, so each item's share of the relevant total is readable
   * without relying on the in-bar SVG label or a tooltip.
   */
  function ranked(
    title: string,
    dimension: string,
    points: AnalyticsTopPoint[],
    selectedYear: number,
    previousYear: number,
  ): ChartNarrative {
    return {
      summary: summaryText(title, points, selectedYear, previousYear),
      table: {
        caption: t('chart.rankCaption', {
          dimension,
          count: points.length,
          year: selectedYear,
          previousYear,
        }),
        columns: [
          dimension,
          String(selectedYear),
          t('chart.table.shareOfTotal'),
          String(previousYear),
        ],
        rows: points.map((point) => [
          point.label,
          formatEur(point.current),
          formatPercent(point.currentPercent),
          formatEur(point.previous),
        ]),
      },
    }
  }

  function monthly(
    title: string,
    points: AnalyticsComparisonPoint[],
    selectedYear: number,
    previousYear: number,
  ): ChartNarrative {
    const currentTotal = total(points, 'current')
    return {
      summary: t('chart.summaryMonthly', {
        title,
        year: selectedYear,
        previousYear,
        total: formatEur(currentTotal),
      }),
      table: buildTable(t('chart.table.month'), points, selectedYear, previousYear),
    }
  }

  return { comparison, monthly, ranked }
}
