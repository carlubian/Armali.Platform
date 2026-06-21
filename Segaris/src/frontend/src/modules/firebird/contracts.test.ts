import { describe, expect, it } from 'vitest'

import {
  firebirdPageSizes,
  firebirdPersonStatuses,
  firebirdRoutePath,
} from '@/app/api/firebird'

import {
  firebirdInteractionRequestSchema,
  firebirdKeys,
  firebirdPersonRequestSchema,
  firebirdUsernameRequestSchema,
} from './contracts'
import {
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parsePeopleState,
  parsePersonDialogState,
  toListQuery,
} from './peopleState'

describe('firebird contracts', () => {
  it('freezes route, statuses, and pagination constants', () => {
    expect(firebirdRoutePath).toBe('/people')
    expect(firebirdPersonStatuses).toEqual([
      'Unknown',
      'Active',
      'Unavailable',
      'Blocked',
    ])
    expect(firebirdPageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('name')
    expect(defaultSortDirection).toBe('asc')
  })

  it('freezes query keys for people, catalogues, avatar, usernames, and interactions', () => {
    expect(firebirdKeys.categories()).toEqual(['firebird', 'categories'])
    expect(firebirdKeys.platforms()).toEqual(['firebird', 'platforms'])
    expect(firebirdKeys.personList({ page: 1 })).toEqual([
      'firebird',
      'people',
      'list',
      { page: 1 },
    ])
    expect(firebirdKeys.person(12)).toEqual(['firebird', 'people', 12])
    expect(firebirdKeys.avatar(12)).toEqual(['firebird', 'people', 12, 'avatar'])
    expect(firebirdKeys.usernames(12)).toEqual(['firebird', 'people', 12, 'usernames'])
    expect(firebirdKeys.interactions(12)).toEqual([
      'firebird',
      'people',
      12,
      'interactions',
    ])
  })

  it('parses URL-backed gallery state with defaults and bounded values', () => {
    const state = parsePeopleState(
      new URLSearchParams(
        'search=ada&category=2&status=Active&visibility=Private&creator=9&sort=birthday&sortDirection=desc&page=3&pageSize=50',
      ),
      9,
    )

    expect(state).toEqual({
      search: 'ada',
      category: 2,
      status: 'Active',
      visibility: 'Private',
      mine: true,
      sort: 'birthday',
      sortDirection: 'desc',
      page: 3,
      pageSize: 50,
    })
    expect(toListQuery(state, 9)).toEqual({
      search: 'ada',
      category: 2,
      status: 'Active',
      visibility: 'Private',
      creator: 9,
      page: 3,
      pageSize: 50,
      sort: 'birthday',
      sortDirection: 'desc',
    })
  })

  it('parses person dialog and sub-dialog state separately from gallery state', () => {
    expect(parsePersonDialogState(new URLSearchParams('newPerson=true'))).toEqual({
      mode: 'create',
    })
    expect(parsePersonDialogState(new URLSearchParams('personId=12'))).toEqual({
      mode: 'edit',
      personId: 12,
    })
    expect(
      parsePersonDialogState(new URLSearchParams('personId=12&usernames=true')),
    ).toEqual({ mode: 'usernames', personId: 12, returnToEdit: false })
    expect(
      parsePersonDialogState(new URLSearchParams('personId=12&interactions=true')),
    ).toEqual({ mode: 'interactions', personId: 12, returnToEdit: false })
    expect(
      parsePersonDialogState(
        new URLSearchParams('personId=12&usernames=true&returnTo=edit'),
      ),
    ).toEqual({ mode: 'usernames', personId: 12, returnToEdit: true })
    expect(parsePersonDialogState(new URLSearchParams('personId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates person birthday and text boundaries', () => {
    const request = firebirdPersonRequestSchema.parse({
      name: '  Ada Lovelace  ',
      categoryId: 1,
      status: 'Unknown',
      birthdayMonth: 2,
      birthdayDay: 29,
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Ada Lovelace')
    expect(request.notes).toBeNull()
    expect(
      firebirdPersonRequestSchema.safeParse({
        ...request,
        birthdayMonth: 2,
        birthdayDay: 30,
      }).success,
    ).toBe(false)
    expect(
      firebirdPersonRequestSchema.safeParse({
        ...request,
        birthdayMonth: 2,
        birthdayDay: null,
      }).success,
    ).toBe(false)
  })

  it('validates username and interaction request boundaries', () => {
    expect(
      firebirdUsernameRequestSchema.parse({
        platformId: 2,
        handle: '  ada@example.test  ',
        notes: '',
      }),
    ).toEqual({ platformId: 2, handle: 'ada@example.test', notes: null })

    expect(
      firebirdInteractionRequestSchema.parse({
        date: '2026-06-21',
        description: '  Met at the library  ',
      }),
    ).toEqual({ date: '2026-06-21', description: 'Met at the library' })
  })
})
