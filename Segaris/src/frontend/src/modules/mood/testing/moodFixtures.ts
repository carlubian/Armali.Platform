import type { MoodDashboard, MoodEntry } from '@/app/api/mood'

/**
 * Shared Mood test builders for the log and dashboard component tests added from
 * Wave 4 onward. Wave 0 ships them empty-of-behavior so later waves extend rather
 * than reinvent fixture shapes.
 */
export function moodEntry(overrides: Partial<MoodEntry> = {}): MoodEntry {
  return {
    id: 1,
    entryDate: '2026-06-15',
    score: 3,
    energy: 'Medium',
    alignment: 'Medium',
    direction: 'Harmony',
    source: 'Internal',
    derivedEmotion: 'calm',
    notes: null,
    createdById: 1,
    createdByName: 'Owner',
    createdAt: '2026-06-15T08:00:00Z',
    updatedById: null,
    updatedByName: null,
    updatedAt: null,
    ...overrides,
  }
}

export function moodDashboard(overrides: Partial<MoodDashboard> = {}): MoodDashboard {
  return {
    scale: 'year',
    period: '2026',
    periodStart: '2026-01-01',
    periodEnd: '2026-12-31',
    previousPeriod: '2025',
    nextPeriod: '2027',
    scoreByDayOfWeek: [],
    scoreByInterval: [],
    distribution: { energy: [], alignment: [], direction: [], source: [] },
    evolution: [],
    ...overrides,
  }
}
