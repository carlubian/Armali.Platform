import {
  Bar,
  CartesianGrid,
  Cell,
  ComposedChart,
  Line,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { analyticsPalette, formatEurCompact } from '../format'
import { AnalyticsTooltip } from './AnalyticsTooltip'
import { axisTick, tooltipCursor } from './chartTheme'
import type { AnalyticsComparisonPoint } from './series'

interface AnalyticsNetBarsChartProps {
  /** Twelve points of net balance (income minus expense) per month. */
  data: AnalyticsComparisonPoint[]
  selectedYear: number
  previousYear: number
}

/**
 * Monthly net balance: current-year bars that turn terracotta when negative,
 * with the previous year as a dashed reference line and a zero baseline.
 */
export function AnalyticsNetBarsChart({
  data,
  selectedYear,
  previousYear,
}: AnalyticsNetBarsChartProps) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <ComposedChart
        data={data}
        margin={{ top: 8, right: 10, left: 2, bottom: 2 }}
        barCategoryGap="26%"
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
        <ReferenceLine y={0} stroke={analyticsPalette.axis} strokeOpacity={0.5} />
        <Bar
          dataKey="current"
          name={String(selectedYear)}
          radius={[4, 4, 0, 0]}
          maxBarSize={26}
          isAnimationActive={false}
        >
          {data.map((point, index) => (
            <Cell
              key={index}
              fill={
                point.current >= 0
                  ? analyticsPalette.netPositive
                  : analyticsPalette.netNegative
              }
            />
          ))}
        </Bar>
        <Line
          type="monotone"
          dataKey="previous"
          name={String(previousYear)}
          stroke={analyticsPalette.ink}
          strokeWidth={2}
          strokeDasharray="4 4"
          strokeOpacity={0.4}
          dot={false}
          isAnimationActive={false}
        />
      </ComposedChart>
    </ResponsiveContainer>
  )
}
