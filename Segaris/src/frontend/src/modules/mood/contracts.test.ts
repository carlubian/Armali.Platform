import { describe, expect, it } from 'vitest'

import {
  moodAlignments,
  moodDashboardRoutePath,
  moodDashboardScales,
  moodDirections,
  moodEnergies,
  moodLogRoutePath,
  moodRoutePath,
  moodSources,
} from '@/app/api/mood'

import { moodEntryRequestSchema, moodKeys } from './contracts'
import {
  currentPeriod,
  defaultDashboardScale,
  nextPeriod,
  parseDashboardState,
  parsePeriod,
  parseScale,
  previousPeriod,
} from './dashboardState'
import {
  addWeeks,
  mondayOf,
  parseEntryDialogState,
  parseSelectedWeek,
  weekDates,
  weekRangeQuery,
} from './logState'

describe('mood contracts', () => {
  it('freezes route paths and criteria vocabularies', () => {
    expect(moodRoutePath).toBe('/mood')
    expect(moodLogRoutePath).toBe('/mood/log')
    expect(moodDashboardRoutePath).toBe('/mood/dashboard')
    expect(moodEnergies).toEqual(['Low', 'Medium', 'High'])
    expect(moodAlignments).toEqual(['Negative', 'Medium', 'Positive'])
    expect(moodDirections).toEqual(['Harmony', 'Defensive', 'Offensive', 'Stability'])
    expect(moodSources).toEqual(['Internal', 'External'])
    expect(moodDashboardScales).toEqual(['year', 'semester', 'quarter', 'month'])
  })

  it('freezes query keys for options, entries, and dashboard', () => {
    expect(moodKeys.options()).toEqual(['mood', 'options'])
    expect(moodKeys.entryRange({ from: '2026-06-15', to: '2026-06-21' })).toEqual([
      'mood',
      'entries',
      'range',
      { from: '2026-06-15', to: '2026-06-21' },
    ])
    expect(moodKeys.entry(7)).toEqual(['mood', 'entries', 7])
    expect(moodKeys.dashboardPeriod({ scale: 'year', period: '2026' })).toEqual([
      'mood',
      'dashboard',
      { scale: 'year', period: '2026' },
    ])
  })

  it('validates entry request boundaries', () => {
    const parsed = moodEntryRequestSchema.parse({
      entryDate: '2026-06-15',
      score: 3,
      energy: 'Medium',
      alignment: 'Positive',
      direction: 'Offensive',
      source: 'Internal',
      notes: '',
    })
    expect(parsed.notes).toBeNull()

    expect(
      moodEntryRequestSchema.safeParse({
        entryDate: '2026-06-15',
        score: 6,
        energy: 'Medium',
        alignment: 'Positive',
        direction: 'Harmony',
        source: 'Internal',
        notes: null,
      }).success,
    ).toBe(false)

    expect(
      moodEntryRequestSchema.safeParse({
        entryDate: '2026-06-15',
        score: 3,
        energy: 'Extreme',
        alignment: 'Positive',
        direction: 'Harmony',
        source: 'Internal',
        notes: null,
      }).success,
    ).toBe(false)

    expect(
      moodEntryRequestSchema.safeParse({
        entryDate: '2026-06-15',
        score: 3,
        energy: 'Medium',
        alignment: 'Positive',
        direction: 'Harmony',
        source: 'Internal',
        notes: 'x'.repeat(1001),
      }).success,
    ).toBe(false)
  })
})

describe('mood log state', () => {
  it('snaps any date to its Monday-to-Sunday week', () => {
    // 2026-06-17 is a Wednesday.
    expect(mondayOf('2026-06-17')).toBe('2026-06-15')
    // 2026-06-21 is a Sunday and stays inside the same week.
    expect(mondayOf('2026-06-21')).toBe('2026-06-15')
    expect(weekDates('2026-06-15')).toEqual([
      '2026-06-15',
      '2026-06-16',
      '2026-06-17',
      '2026-06-18',
      '2026-06-19',
      '2026-06-20',
      '2026-06-21',
    ])
    expect(weekRangeQuery('2026-06-15')).toEqual({
      from: '2026-06-15',
      to: '2026-06-21',
    })
    expect(addWeeks('2026-06-15', -1)).toBe('2026-06-08')
  })

  it('defaults the selected week to today and honours a valid week parameter', () => {
    expect(parseSelectedWeek(new URLSearchParams(''), '2026-06-17')).toBe('2026-06-15')
    expect(
      parseSelectedWeek(new URLSearchParams('week=2026-01-03'), '2026-06-17'),
    ).toBe('2025-12-29')
    expect(
      parseSelectedWeek(new URLSearchParams('week=not-a-date'), '2026-06-17'),
    ).toBe('2026-06-15')
  })

  it('parses entry dialog state from URL parameters', () => {
    expect(parseEntryDialogState(new URLSearchParams('newEntry=true'))).toEqual({
      mode: 'create',
    })
    expect(parseEntryDialogState(new URLSearchParams('entryId=12'))).toEqual({
      mode: 'edit',
      entryId: 12,
    })
    expect(parseEntryDialogState(new URLSearchParams('entryId=0'))).toEqual({
      mode: 'closed',
    })
    expect(parseEntryDialogState(new URLSearchParams(''))).toEqual({ mode: 'closed' })
  })
})

describe('mood dashboard state', () => {
  it('parses scales and defaults to the current year', () => {
    expect(defaultDashboardScale).toBe('year')
    expect(parseScale('quarter')).toBe('quarter')
    expect(parseScale('weekly')).toBeNull()
    expect(currentPeriod('year', '2026-06-17')).toBe('2026')
    expect(currentPeriod('semester', '2026-09-01')).toBe('2026-S2')
    expect(currentPeriod('quarter', '2026-05-01')).toBe('2026-Q2')
    expect(currentPeriod('month', '2026-06-17')).toBe('2026-06')
  })

  it('normalizes and navigates period tokens with year rollover', () => {
    expect(parsePeriod('quarter', '2026-Q3')).toBe('2026-Q3')
    expect(parsePeriod('quarter', '2026-Q5')).toBeNull()
    expect(parsePeriod('month', '2026-1')).toBeNull()
    expect(previousPeriod('quarter', '2026-Q1')).toBe('2025-Q4')
    expect(nextPeriod('semester', '2026-S2')).toBe('2027-S1')
    expect(nextPeriod('month', '2026-12')).toBe('2027-01')
  })

  it('resolves dashboard state from the URL with fallbacks', () => {
    expect(parseDashboardState(new URLSearchParams(''), '2026-06-17')).toEqual({
      scale: 'year',
      period: '2026',
    })
    expect(
      parseDashboardState(
        new URLSearchParams('scale=month&period=2026-03'),
        '2026-06-17',
      ),
    ).toEqual({ scale: 'month', period: '2026-03' })
    // A period token that does not match the selected scale falls back to current.
    expect(
      parseDashboardState(
        new URLSearchParams('scale=quarter&period=2026-13'),
        '2026-06-17',
      ),
    ).toEqual({ scale: 'quarter', period: '2026-Q2' })
  })
})
