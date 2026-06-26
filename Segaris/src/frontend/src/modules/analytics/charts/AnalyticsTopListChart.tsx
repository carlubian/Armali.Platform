import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

import { analyticsPalette, formatEur, formatPercent } from '../format'
import { AnalyticsTooltip } from './AnalyticsTooltip'
import { axisTick, previousBarProps, tooltipCursor } from './chartTheme'
import type { AnalyticsTopPoint } from './series'

interface AnalyticsTopListChartProps {
  /** Ranked points, highest first; each carries its share of the total. */
  data: AnalyticsTopPoint[]
  selectedYear: number
  previousYear: number
  color: string
}

interface BarLabelProps {
  x?: number | string
  y?: number | string
  width?: number | string
  height?: number | string
  index?: number
}

/**
 * Horizontal ranked bars (a top-N list). Each current-year bar is labelled with
 * its EUR amount and share of the total to the right, with the previous year
 * faded behind it.
 */
export function AnalyticsTopListChart({
  data,
  selectedYear,
  previousYear,
  color,
}: AnalyticsTopListChartProps) {
  // Recharts calls this with positional props; it is a render callback, not a
  // React component, so prop-types validation does not apply.
  /* eslint-disable react/prop-types */
  const renderShareLabel = (props: BarLabelProps) => {
    const { index } = props
    const x = Number(props.x ?? 0)
    const y = Number(props.y ?? 0)
    const width = Number(props.width ?? 0)
    const height = Number(props.height ?? 0)
    const point = index != null ? data[index] : undefined
    if (point == null) return null
    return (
      <text
        x={x + width + 8}
        y={y + height / 2}
        dominantBaseline="central"
        fontFamily="League Spartan"
        fontWeight={600}
        fontSize={11.5}
        fill={analyticsPalette.ink}
      >
        {formatEur(point.current)} · {formatPercent(point.currentPercent)}
      </text>
    )
  }
  /* eslint-enable react/prop-types */

  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart
        data={data}
        layout="vertical"
        margin={{ top: 4, right: 96, left: 6, bottom: 2 }}
        barGap={2}
        barCategoryGap="30%"
      >
        <CartesianGrid horizontal={false} stroke="rgba(124, 110, 86, 0.18)" />
        <XAxis type="number" hide />
        <YAxis
          type="category"
          dataKey="label"
          tick={{
            ...axisTick,
            fontFamily: 'League Spartan',
            fontWeight: 600,
            fill: analyticsPalette.ink,
          }}
          tickLine={false}
          axisLine={false}
          width={104}
        />
        <Tooltip cursor={tooltipCursor} content={<AnalyticsTooltip />} />
        <Bar
          dataKey="previous"
          name={String(previousYear)}
          radius={[0, 4, 4, 0]}
          maxBarSize={11}
          isAnimationActive={false}
          {...previousBarProps(color)}
        />
        <Bar
          dataKey="current"
          name={String(selectedYear)}
          fill={color}
          radius={[0, 4, 4, 0]}
          maxBarSize={11}
          isAnimationActive={false}
        >
          <LabelList dataKey="current" content={renderShareLabel} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
