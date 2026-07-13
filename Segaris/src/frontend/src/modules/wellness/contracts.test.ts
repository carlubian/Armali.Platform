import { describe, expect, it } from 'vitest'

import {
  wellnessCategories,
  wellnessRoutePath,
  wellnessTaskNameMaxLength,
} from '@/app/api/wellness'

import { wellnessKeys, wellnessTaskRequestSchema } from './contracts'
import { toWellnessDaysQuery } from './wellnessState'

describe('wellness contracts', () => {
  it('freezes the route path, name bound, and category vocabulary', () => {
    expect(wellnessRoutePath).toBe('/wellness')
    expect(wellnessTaskNameMaxLength).toBe(200)
    expect(wellnessCategories).toEqual([
      'HealthAndBody',
      'MindAndSleep',
      'PeopleAndWork',
    ])
  })

  it('freezes query keys for today, tasks, and the days range', () => {
    expect(wellnessKeys.today()).toEqual(['wellness', 'today'])
    expect(wellnessKeys.tasks()).toEqual(['wellness', 'tasks'])
    expect(wellnessKeys.days({ from: '2026-07-06', to: '2026-07-12' })).toEqual([
      'wellness',
      'days',
      { from: '2026-07-06', to: '2026-07-12' },
    ])
  })

  it('builds the Mood-facing days query only for a valid inclusive range', () => {
    expect(toWellnessDaysQuery('2026-07-06', '2026-07-12')).toEqual({
      from: '2026-07-06',
      to: '2026-07-12',
    })
    expect(toWellnessDaysQuery('2026-07-12', '2026-07-06')).toBeNull()
    expect(toWellnessDaysQuery('', '2026-07-12')).toBeNull()
  })

  it('validates task request boundaries', () => {
    expect(
      wellnessTaskRequestSchema.parse({
        name: ' Drink water ',
        category: 'HealthAndBody',
      }).name,
    ).toBe('Drink water')
    expect(
      wellnessTaskRequestSchema.safeParse({ name: '', category: 'HealthAndBody' })
        .success,
    ).toBe(false)
    expect(
      wellnessTaskRequestSchema.safeParse({ name: 'Stretch', category: 'Focus' })
        .success,
    ).toBe(false)
  })
})
