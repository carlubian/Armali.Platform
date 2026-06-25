import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { formatEurCompact } from '../format'
import { AnalyticsTooltip } from './AnalyticsTooltip'
import { axisTick } from './chartTheme'
import type { AnalyticsComparisonPoint } from './series'

interface AnalyticsMonthlyLineChartProps {
  /** Twelve points, one per month, labelled with the short month name. */
  data: AnalyticsComparisonPoint[]
  selectedYear: number
  previousYear: number
  color: string
}

/**
 * Monthly trend line comparing the selected year against the previous year. The
 * previous year is a faded dashed line behind the solid current-year line.
 */
export function AnalyticsMonthlyLineChart({
  data,
  selectedYear,
  previousYear,
  color,
}: AnalyticsMonthlyLineChartProps) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <LineChart data={data} margin={{ top: 8, right: 10, left: 2, bottom: 2 }}>
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
        <Tooltip content={<AnalyticsTooltip />} />
        <Line
          type="monotone"
          dataKey="previous"
          name={String(previousYear)}
          stroke={color}
          strokeWidth={2}
          strokeDasharray="4 4"
          strokeOpacity={0.45}
          dot={false}
          isAnimationActive={false}
        />
        <Line
          type="monotone"
          dataKey="current"
          name={String(selectedYear)}
          stroke={color}
          strokeWidth={2.75}
          dot={{ r: 2.5, fill: color, strokeWidth: 0 }}
          activeDot={{ r: 4 }}
          isAnimationActive={false}
        />
      </LineChart>
    </ResponsiveContainer>
  )
}
