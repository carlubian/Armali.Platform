import { describe, expect, it } from 'vitest'

import {
  calendarIndicatorPriority,
  calendarInitialProjectionSources,
  calendarMaximumRangeDays,
  calendarRoutePath,
  calendarSourceModules,
  calendarSourceTypes,
  calendarVisualFamilies,
} from '@/app/api/calendar'

import {
  addCalendarMonths,
  formatCalendarMonth,
  formatCivilDate,
  getVisibleCalendarGrid,
  parseCalendarDialogState,
  parseCalendarState,
  resolveCalendarMonth,
  toCalendarEntriesQuery,
} from './calendarState'
import { calendarDailyNoteRequestSchema, calendarKeys } from './contracts'

describe('calendar contracts', () => {
  it('freezes route, range, source, visual-family, and indicator contracts', () => {
    expect(calendarRoutePath).toBe('/calendar')
    expect(calendarMaximumRangeDays).toBe(366)
    expect(calendarInitialProjectionSources).toEqual([
      'firebird',
      'travel',
      'inventory',
      'assets',
      'maintenance',
      'processes',
    ])
    expect(calendarSourceModules).toEqual([
      'calendar',
      'firebird',
      'travel',
      'inventory',
      'assets',
      'maintenance',
      'processes',
    ])
    expect(calendarSourceTypes).toEqual([
      'dailyNote',
      'birthday',
      'trip',
      'inventoryOrderExpectedReceipt',
      'assetExpectedEndOfLife',
      'maintenanceTaskDue',
      'processStepDue',
    ])
    expect(calendarVisualFamilies).toEqual(['Birthday', 'Travel', 'Note', 'Other'])
    expect(calendarIndicatorPriority).toEqual([
      'Travel',
      'Birthday',
      'Note',
      'Other',
    ])
  })

  it('freezes query keys for entries, ranges, and note detail', () => {
    expect(
      calendarKeys.entriesRange({
        from: '2026-06-01',
        to: '2026-06-30',
        sourceModule: ['travel'],
      }),
    ).toEqual([
      'calendar',
      'entries',
      { from: '2026-06-01', to: '2026-06-30', sourceModule: ['travel'] },
    ])
    expect(calendarKeys.notesRange({ from: '2026-06-01', to: '2026-06-30' })).toEqual(
      ['calendar', 'notes', { from: '2026-06-01', to: '2026-06-30' }],
    )
    expect(calendarKeys.note(12)).toEqual(['calendar', 'notes', 12])
  })

  it('parses URL-backed month, day, filters, and dialog state', () => {
    const params = new URLSearchParams(
      'month=2026-06&day=2026-06-24&sourceModule=travel&sourceModule=capex&visualFamily=Travel&visualFamily=Finance&newNote=true',
    )

    const state = parseCalendarState(params)

    expect(state).toEqual({
      month: '2026-06',
      day: '2026-06-24',
      filters: {
        sourceModules: ['travel'],
        visualFamilies: ['Travel'],
      },
    })
    expect(parseCalendarDialogState(params)).toEqual({ mode: 'createNote' })
    expect(parseCalendarDialogState(new URLSearchParams('noteId=12'))).toEqual({
      mode: 'editNote',
      noteId: 12,
    })
  })

  it('builds the entries query from visible grid range and filters', () => {
    const state = parseCalendarState(
      new URLSearchParams('sourceModule=travel&visualFamily=Travel'),
    )

    expect(toCalendarEntriesQuery('2026-06-01', '2026-06-30', state)).toEqual({
      from: '2026-06-01',
      to: '2026-06-30',
      sourceModule: ['travel'],
      visualFamily: ['Travel'],
    })
  })

  it('calculates Monday-first month grids including adjacent days', () => {
    const june = getVisibleCalendarGrid('2026-06')

    expect(june).toHaveLength(35)
    expect(june[0]).toEqual({ date: '2026-06-01', inMonth: true })
    expect(june.at(-1)).toEqual({ date: '2026-07-05', inMonth: false })

    const july = getVisibleCalendarGrid('2026-07')
    expect(july[0]).toEqual({ date: '2026-06-29', inMonth: false })
    expect(july.at(-1)).toEqual({ date: '2026-08-02', inMonth: false })
  })

  it('formats, resolves, and increments civil months without time components', () => {
    const today = new Date(2026, 5, 24)

    expect(formatCivilDate(today)).toBe('2026-06-24')
    expect(formatCalendarMonth(today)).toBe('2026-06')
    expect(resolveCalendarMonth('current', today)).toBe('2026-06')
    expect(addCalendarMonths('2026-01', -1)).toBe('2025-12')
    expect(addCalendarMonths('2026-12', 1)).toBe('2027-01')
  })

  it('validates daily note request boundaries and private default', () => {
    const request = calendarDailyNoteRequestSchema.parse({
      date: '2026-06-24',
      title: '  Appointment notes  ',
      body: '  Bring documents  ',
    })

    expect(request).toEqual({
      date: '2026-06-24',
      title: 'Appointment notes',
      body: 'Bring documents',
      visibility: 'Private',
    })
    expect(
      calendarDailyNoteRequestSchema.safeParse({
        ...request,
        title: 'x'.repeat(201),
      }).success,
    ).toBe(false)
    expect(
      calendarDailyNoteRequestSchema.safeParse({ ...request, body: '' }).success,
    ).toBe(false)
    expect(
      calendarDailyNoteRequestSchema.safeParse({
        ...request,
        body: 'x'.repeat(4001),
      }).success,
    ).toBe(false)
  })
})
