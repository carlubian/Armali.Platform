import { describe, expect, it } from 'vitest'

import {
  analyticsPalette,
  formatDelta,
  formatEur,
  formatEurCompact,
  formatPercent,
  monthShortLabel,
  yearOverYear,
} from './format'

describe('analytics formatting', () => {
  it('rounds amounts to whole euros', () => {
    expect(formatEur(1234.6)).toBe('€1,235')
    expect(formatEur(0)).toBe('€0')
  })

  it('compacts large amounts for chart axes', () => {
    expect(formatEurCompact(450)).toBe('€450')
    expect(formatEurCompact(1500)).toBe('€1.5k')
    expect(formatEurCompact(12000)).toBe('€12k')
    expect(formatEurCompact(-1500)).toBe('-€1.5k')
  })

  it('formats percentages with extra precision below ten percent', () => {
    expect(formatPercent(0.06)).toBe('6.0%')
    expect(formatPercent(0.042)).toBe('4.2%')
    expect(formatPercent(0.5)).toBe('50%')
  })

  it('signs year-over-year deltas', () => {
    expect(formatDelta(0.12)).toBe('+12%')
    expect(formatDelta(-0.034)).toBe('-3.4%')
    expect(formatDelta(0)).toBe('0.0%')
  })

  it('guards the year-over-year ratio against a zero baseline', () => {
    expect(yearOverYear(110, 100)).toBeCloseTo(0.1)
    expect(yearOverYear(90, 100)).toBeCloseTo(-0.1)
    expect(yearOverYear(100, 0)).toBeNull()
  })

  it('labels months by their 1-based number', () => {
    expect(monthShortLabel(1)).toBe('Jan')
    expect(monthShortLabel(12)).toBe('Dec')
  })

  it('exposes concrete hex colours for SVG fills', () => {
    expect(analyticsPalette.expense).toMatch(/^#[0-9a-f]{6}$/)
    expect(analyticsPalette.income).toMatch(/^#[0-9a-f]{6}$/)
  })
})
