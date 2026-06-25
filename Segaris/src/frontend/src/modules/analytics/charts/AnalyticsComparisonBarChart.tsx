import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { formatEurCompact } from '../format'
import { AnalyticsTooltip } from './AnalyticsTooltip'
import { axisTick, previousBarProps, tooltipCursor } from './chartTheme'
import type { AnalyticsComparisonPoint } from './series'

interface AnalyticsComparisonBarChartProps {
  data: AnalyticsComparisonPoint[]
  selectedYear: number
  previousYear: number
  color: string
  barSize?: number
}

/**
 * Grouped vertical bars comparing a categorical dimension across the selected
 * year and the previous year. The previous-year bar is drawn faded behind the
 * solid current-year bar.
 */
export function AnalyticsComparisonBarChart({
  data,
  selectedYear,
  previousYear,
  color,
  barSize = 17,
}: AnalyticsComparisonBarChartProps) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart
        data={data}
        margin={{ top: 8, right: 6, left: 2, bottom: 2 }}
        barGap={3}
        barCategoryGap="22%"
      >
        <CartesianGrid vertical={false} stroke="rgba(124, 110, 86, 0.18)" />
        <XAxis
          dataKey="label"
          tick={axisTick}
          tickLine={false}
          axisLine={{ stroke: 'rgba(124, 110, 86, 0.18)' }}
          interval={0}
          tickMargin={8}
        />
        <YAxis
          tick={axisTick}
          tickLine={false}
          axisLine={false}
          width={46}
          tickFormatter={formatEurCompact}
        />
        <Tooltip cursor={tooltipCursor} content={<AnalyticsTooltip />} />
        <Bar
          dataKey="previous"
          name={String(previousYear)}
          radius={[5, 5, 0, 0]}
          maxBarSize={barSize}
          isAnimationActive={false}
          {...previousBarProps(color)}
        />
        <Bar
          dataKey="current"
          name={String(selectedYear)}
          fill={color}
          radius={[5, 5, 0, 0]}
          maxBarSize={barSize}
          isAnimationActive={false}
        />
      </BarChart>
    </ResponsiveContainer>
  )
}
