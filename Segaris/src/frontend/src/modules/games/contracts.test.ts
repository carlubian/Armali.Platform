import { describe, expect, it } from 'vitest'

import {
  gamePlatforms,
  gamesPageSizes,
  gamesRoutePath,
  playthroughStatuses,
  sectionColors,
} from '@/app/api/games'

import {
  gameRequestSchema,
  gamesKeys,
  goalRequestSchema,
  playthroughRequestSchema,
  sectionRequestSchema,
} from './contracts'
import {
  defaultPageSize,
  defaultPlaythroughSort,
  defaultSortDirection,
  parseManageSections,
  parsePlaythroughDialogState,
  parsePlaythroughListState,
  parseSelectedSection,
  toPlaythroughListQuery,
} from './gamesState'

describe('games contracts', () => {
  it('freezes route, pagination, sort defaults, and enum vocabularies', () => {
    expect(gamesRoutePath).toBe('/games')
    expect(gamesPageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPlaythroughSort).toBe('name')
    expect(defaultSortDirection).toBe('asc')
    expect(defaultPageSize).toBe(25)
    expect(gamePlatforms).toEqual([
      'PC',
      'Console',
      'Mobile',
      'BoardGame',
      'TabletopRpg',
      'Other',
    ])
    expect(playthroughStatuses).toEqual(['Planning', 'Active', 'Completed'])
    expect(sectionColors).toEqual([
      'Blue',
      'Green',
      'Amber',
      'Red',
      'Purple',
      'Pink',
      'Teal',
      'Indigo',
      'Slate',
      'Orange',
    ])
  })

  it('freezes query keys for catalogue, lists, sections, and goals', () => {
    expect(gamesKeys.games()).toEqual(['games', 'games'])
    expect(gamesKeys.playthroughList({ page: 1 })).toEqual([
      'games',
      'playthroughs',
      'list',
      { page: 1 },
    ])
    expect(gamesKeys.sections(12)).toEqual(['games', 'playthroughs', 12, 'sections'])
    expect(gamesKeys.goals(12, 3)).toEqual([
      'games',
      'playthroughs',
      12,
      'sections',
      3,
      'goals',
    ])
  })

  it('parses URL-backed collection state into a list query', () => {
    const state = parsePlaythroughListState(
      new URLSearchParams(
        'search=iron&game=3&platform=Console&status=Active&tag=story&visibility=Private&creator=9&sort=game&sortDirection=desc&page=2&pageSize=50',
      ),
      9,
    )

    expect(toPlaythroughListQuery(state, 9)).toEqual({
      search: 'iron',
      game: 3,
      platform: 'Console',
      status: 'Active',
      tag: 'story',
      creator: 9,
      visibility: 'Private',
      page: 2,
      pageSize: 50,
      sort: 'game',
      sortDirection: 'desc',
    })
  })

  it('parses playthrough dialog and progress-page section state', () => {
    expect(
      parsePlaythroughDialogState(new URLSearchParams('newPlaythrough=true')),
    ).toEqual({ mode: 'createPlaythrough' })
    expect(
      parsePlaythroughDialogState(new URLSearchParams('playthroughId=12')),
    ).toEqual({
      mode: 'editPlaythrough',
      playthroughId: 12,
    })
    expect(parsePlaythroughDialogState(new URLSearchParams())).toEqual({
      mode: 'closed',
    })
    expect(parseSelectedSection(new URLSearchParams('sectionId=45'))).toBe(45)
    expect(parseSelectedSection(new URLSearchParams())).toBeNull()
    expect(parseManageSections(new URLSearchParams('manageSections=true'))).toBe(true)
  })

  it('validates game, playthrough, section, and goal request boundaries', () => {
    expect(gameRequestSchema.parse({ name: ' Elden Ring ', platform: 'PC' }).name).toBe(
      'Elden Ring',
    )
    expect(gameRequestSchema.safeParse({ name: '', platform: 'PC' }).success).toBe(
      false,
    )
    expect(
      gameRequestSchema.safeParse({ name: 'X', platform: 'Handheld' }).success,
    ).toBe(false)

    const playthrough = playthroughRequestSchema.parse({
      name: ' Ironman ',
      gameId: 3,
      startYear: 2026,
      startMonth: 7,
      status: 'Active',
      tags: ['Story', 'Hard'],
      visibility: 'Private',
    })
    expect(playthrough.name).toBe('Ironman')
    expect(
      playthroughRequestSchema.safeParse({ ...playthrough, startMonth: 13 }).success,
    ).toBe(false)
    expect(
      playthroughRequestSchema.safeParse({ ...playthrough, startYear: 0 }).success,
    ).toBe(false)

    expect(sectionRequestSchema.parse({ name: ' Missions ', color: 'Blue' }).name).toBe(
      'Missions',
    )
    expect(sectionRequestSchema.safeParse({ name: 'X', color: 'Cyan' }).success).toBe(
      false,
    )

    expect(goalRequestSchema.parse({ text: ' Beat boss ' }).text).toBe('Beat boss')
    expect(goalRequestSchema.safeParse({ text: '' }).success).toBe(false)
  })
})
