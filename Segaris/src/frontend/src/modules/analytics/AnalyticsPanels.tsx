import type { UseQueryResult } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import type {
  AnalyticsAverageAmountPoint,
  AnalyticsChartResponse,
  AnalyticsGroupedAmountPoint,
  AnalyticsInventoryResponse,
  AnalyticsMoneySeriesPoint,
  AnalyticsOverviewResponse,
  AnalyticsTab,
  AnalyticsTopAmountPoint,
  AnalyticsViewResponse,
} from '@/app/api/analytics'

import {
  AnalyticsConfigurationIncomplete,
  AnalyticsError,
  AnalyticsLoading,
} from './AnalyticsStates'
import {
  AnalyticsChartCard,
  AnalyticsComparisonBarChart,
  AnalyticsLegend,
  AnalyticsMonthlyLineChart,
  AnalyticsNetBarsChart,
  AnalyticsTopListChart,
  type AnalyticsComparisonPoint,
  type AnalyticsTopPoint,
} from './charts'
import { analyticsPalette, monthShortLabel } from './format'
import { useChartNarrative } from './narrative'
import {
  useAnalyticsCapex,
  useAnalyticsCrossModule,
  useAnalyticsInventory,
  useAnalyticsOpex,
  useAnalyticsOverview,
  useAnalyticsTravel,
} from './queries'

interface AnalyticsResponse {
  missingExchangeRateCurrencyCodes: string[]
}

interface PanelProps {
  year: number
  onConfigure?: () => void
}

/**
 * Resolves the four shared tab states — loading, error, configuration
 * incomplete (a currency without a current EUR rate), and ready — so each tab
 * panel only describes its charts.
 */
function AnalyticsAsync<T extends AnalyticsResponse>({
  query,
  onConfigure,
  children,
}: {
  query: UseQueryResult<T>
  onConfigure?: () => void
  children: (data: T) => ReactNode
}) {
  if (query.isPending) return <AnalyticsLoading />
  if (query.isError || query.data == null) {
    return <AnalyticsError onRetry={() => void query.refetch()} />
  }
  if (query.data.missingExchangeRateCurrencyCodes.length > 0) {
    return (
      <AnalyticsConfigurationIncomplete
        currencyCodes={query.data.missingExchangeRateCurrencyCodes}
        onConfigure={onConfigure}
      />
    )
  }
  return <>{children(query.data)}</>
}

function TabHead({ tab }: { tab: AnalyticsTab }) {
  const { t } = useTranslation('analytics')
  const scope = t(`tab.${tab}.scope`, { defaultValue: '' })
  return (
    <div className="an-tabhead">
      <div className="an-tabhead__txt">
        <div className="armali-eyebrow">{t(`tab.${tab}.eyebrow`)}</div>
        <h2>{t(`tab.${tab}.title`)}</h2>
        <p>{t(`tab.${tab}.description`)}</p>
      </div>
      {scope !== '' && <span className="an-tabhead__scope">{scope}</span>}
    </div>
  )
}

function isIncomeChart(chartId: string): boolean {
  return chartId.toLowerCase().includes('income')
}

function toComparison(
  points: AnalyticsGroupedAmountPoint[],
): AnalyticsComparisonPoint[] {
  return points.map((point) => ({
    label: point.label,
    current: point.selectedYearAmountEur,
    previous: point.previousYearAmountEur,
  }))
}

function toAverage(points: AnalyticsAverageAmountPoint[]): AnalyticsComparisonPoint[] {
  return points.map((point) => ({
    label: point.label,
    current: point.selectedYearAverageEur,
    previous: point.previousYearAverageEur,
  }))
}

function toTop(points: AnalyticsTopAmountPoint[]): AnalyticsTopPoint[] {
  // Backend percentages are 0–100; chart formatting expects a 0–1 fraction.
  return points.map((point) => ({
    label: point.label,
    current: point.selectedYearAmountEur,
    previous: point.previousYearAmountEur,
    currentPercent: point.selectedYearPercent / 100,
    previousPercent: point.previousYearPercent / 100,
  }))
}

/** A categorical grouped-bar card driven entirely by the chart's data. */
function GroupedChartCard({
  chartId,
  points,
  selectedYear,
  previousYear,
}: {
  chartId: string
  points: AnalyticsComparisonPoint[]
  selectedYear: number
  previousYear: number
}) {
  const { t } = useTranslation('analytics')
  const { comparison } = useChartNarrative()
  const color = isIncomeChart(chartId)
    ? analyticsPalette.income
    : analyticsPalette.expense
  const title = t(`charts.${chartId}`, { defaultValue: chartId })
  const { summary, table } = comparison(
    title,
    title,
    points,
    selectedYear,
    previousYear,
  )
  return (
    <AnalyticsChartCard
      title={title}
      summary={summary}
      table={table}
      isEmpty={points.length === 0}
      legend={
        <AnalyticsLegend
          selectedYear={selectedYear}
          previousYear={previousYear}
          color={color}
        />
      }
    >
      <AnalyticsComparisonBarChart
        data={points}
        selectedYear={selectedYear}
        previousYear={previousYear}
        color={color}
      />
    </AnalyticsChartCard>
  )
}

type GroupedView = AnalyticsViewResponse<
  AnalyticsChartResponse<AnalyticsGroupedAmountPoint>
>

/** Shared body for the four grouped-bar tabs (Capex, Opex, Travel, Cross-module). */
function GroupedTabBody({
  tab,
  query,
  onConfigure,
}: {
  tab: AnalyticsTab
  query: UseQueryResult<GroupedView>
  onConfigure?: () => void
}) {
  return (
    <AnalyticsAsync query={query} onConfigure={onConfigure}>
      {(data) => (
        <>
          <TabHead tab={tab} />
          <div className="an-grid an-grid--2">
            {data.charts.map((chart) => (
              <GroupedChartCard
                key={chart.chartId}
                chartId={chart.chartId}
                points={toComparison(chart.points)}
                selectedYear={data.selectedYear}
                previousYear={data.previousYear}
              />
            ))}
          </div>
        </>
      )}
    </AnalyticsAsync>
  )
}

export function AnalyticsCapexPanel({ year, onConfigure }: PanelProps) {
  return (
    <GroupedTabBody
      tab="capex"
      query={useAnalyticsCapex(year)}
      onConfigure={onConfigure}
    />
  )
}

export function AnalyticsOpexPanel({ year, onConfigure }: PanelProps) {
  return (
    <GroupedTabBody
      tab="opex"
      query={useAnalyticsOpex(year)}
      onConfigure={onConfigure}
    />
  )
}

export function AnalyticsTravelPanel({ year, onConfigure }: PanelProps) {
  return (
    <GroupedTabBody
      tab="travel"
      query={useAnalyticsTravel(year)}
      onConfigure={onConfigure}
    />
  )
}

export function AnalyticsCrossModulePanel({ year, onConfigure }: PanelProps) {
  return (
    <GroupedTabBody
      tab="cross-module"
      query={useAnalyticsCrossModule(year)}
      onConfigure={onConfigure}
    />
  )
}

function OverviewChartCard({
  chart,
  selectedYear,
  previousYear,
  language,
}: {
  chart: AnalyticsChartResponse<AnalyticsMoneySeriesPoint>
  selectedYear: number
  previousYear: number
  language: string
}) {
  const { t } = useTranslation('analytics')
  const { monthly } = useChartNarrative()
  const points: AnalyticsComparisonPoint[] = chart.points.map((point) => ({
    label: monthShortLabel(point.month, language),
    current: point.selectedYearAmountEur,
    previous: point.previousYearAmountEur,
  }))
  const isNet = chart.chartId.toLowerCase().includes('net')
  const color = isNet
    ? analyticsPalette.netPositive
    : isIncomeChart(chart.chartId)
      ? analyticsPalette.income
      : analyticsPalette.expense
  const title = t(`charts.${chart.chartId}`, { defaultValue: chart.chartId })
  const { summary, table } = monthly(title, points, selectedYear, previousYear)
  return (
    <AnalyticsChartCard
      title={title}
      height="lg"
      className="an-span-2"
      summary={summary}
      table={table}
      legend={
        <AnalyticsLegend
          selectedYear={selectedYear}
          previousYear={previousYear}
          color={color}
        />
      }
    >
      {isNet ? (
        <AnalyticsNetBarsChart
          data={points}
          selectedYear={selectedYear}
          previousYear={previousYear}
        />
      ) : (
        <AnalyticsMonthlyLineChart
          data={points}
          selectedYear={selectedYear}
          previousYear={previousYear}
          color={color}
        />
      )}
    </AnalyticsChartCard>
  )
}

export function AnalyticsOverviewPanel({ year, onConfigure }: PanelProps) {
  const { i18n } = useTranslation('analytics')
  const query: UseQueryResult<AnalyticsOverviewResponse> = useAnalyticsOverview(year)
  return (
    <AnalyticsAsync query={query} onConfigure={onConfigure}>
      {(data) => (
        <>
          <TabHead tab="overview" />
          <div className="an-grid an-grid--1">
            {data.charts.map((chart) => (
              <OverviewChartCard
                key={chart.chartId}
                chart={chart}
                selectedYear={data.selectedYear}
                previousYear={data.previousYear}
                language={i18n.language}
              />
            ))}
          </div>
        </>
      )}
    </AnalyticsAsync>
  )
}

function InventoryTopCard({
  chart,
  selectedYear,
  previousYear,
}: {
  chart: AnalyticsChartResponse<AnalyticsTopAmountPoint>
  selectedYear: number
  previousYear: number
}) {
  const { t } = useTranslation('analytics')
  const { comparison } = useChartNarrative()
  const points = toTop(chart.points)
  const title = t(`charts.${chart.chartId}`, { defaultValue: chart.chartId })
  const { summary, table } = comparison(
    title,
    title,
    points,
    selectedYear,
    previousYear,
  )
  return (
    <AnalyticsChartCard
      title={title}
      height="lg"
      summary={summary}
      table={table}
      isEmpty={points.length === 0}
      legend={
        <AnalyticsLegend
          selectedYear={selectedYear}
          previousYear={previousYear}
          color={analyticsPalette.expense}
        />
      }
    >
      <AnalyticsTopListChart
        data={points}
        selectedYear={selectedYear}
        previousYear={previousYear}
        color={analyticsPalette.expense}
      />
    </AnalyticsChartCard>
  )
}

export function AnalyticsInventoryPanel({ year, onConfigure }: PanelProps) {
  const query: UseQueryResult<AnalyticsInventoryResponse> = useAnalyticsInventory(year)
  return (
    <AnalyticsAsync query={query} onConfigure={onConfigure}>
      {(data) => (
        <>
          <TabHead tab="inventory" />
          <div className="an-grid an-grid--2">
            {data.groupedCharts.map((chart) => (
              <GroupedChartCard
                key={chart.chartId}
                chartId={chart.chartId}
                points={toComparison(chart.points)}
                selectedYear={data.selectedYear}
                previousYear={data.previousYear}
              />
            ))}
          </div>
          <div className="an-grid an-grid--2">
            {data.topCharts.map((chart) => (
              <InventoryTopCard
                key={chart.chartId}
                chart={chart}
                selectedYear={data.selectedYear}
                previousYear={data.previousYear}
              />
            ))}
          </div>
          <div className="an-grid an-grid--1">
            {data.averageCharts.map((chart) => (
              <GroupedChartCard
                key={chart.chartId}
                chartId={chart.chartId}
                points={toAverage(chart.points)}
                selectedYear={data.selectedYear}
                previousYear={data.previousYear}
              />
            ))}
          </div>
        </>
      )}
    </AnalyticsAsync>
  )
}
