import { analyticsPalette } from '../format'

/** Shared axis tick styling for every Analytics chart. */
export const axisTick = {
  fill: analyticsPalette.axis,
  fontSize: 11,
  fontFamily: 'Nunito',
} as const

/** Faded, dashed-outline appearance for the previous-year bars. */
export function previousBarProps(color: string) {
  return {
    fill: color,
    fillOpacity: 0.3,
  }
}

/** Tooltip cursor highlight tuned to the warm-ink palette. */
export const tooltipCursor = { fill: 'rgba(124, 110, 86, 0.07)' } as const
