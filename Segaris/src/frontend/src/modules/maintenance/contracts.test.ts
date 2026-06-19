import { describe, expect, it } from 'vitest'

import { maintenancePageSizes, maintenanceRoutePath } from '@/app/api/maintenance'

import { maintenanceKeys, maintenanceTaskRequestSchema } from './contracts'
import {
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parseMaintenanceDialogState,
  parseMaintenanceState,
  toListQuery,
} from './maintenanceState'

describe('maintenance contracts', () => {
  it('freezes route and pagination constants', () => {
    expect(maintenanceRoutePath).toBe('/maintenance')
    expect(maintenancePageSizes).toEqual([10, 25, 50, 100])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('dueDate')
    expect(defaultSortDirection).toBe('asc')
  })

  it('freezes query keys for tasks, types, and attachments', () => {
    expect(maintenanceKeys.types()).toEqual(['maintenance', 'types'])
    expect(maintenanceKeys.taskList({ page: 1 })).toEqual([
      'maintenance',
      'tasks',
      'list',
      { page: 1 },
    ])
    expect(maintenanceKeys.task(7)).toEqual(['maintenance', 'tasks', 7])
    expect(maintenanceKeys.taskAttachments(7)).toEqual([
      'maintenance',
      'tasks',
      7,
      'attachments',
    ])
  })

  it('parses URL-backed table state with defaults and bounded values', () => {
    const state = parseMaintenanceState(
      new URLSearchParams(
        'search=filter&type=4&status=InProgress&priority=High&asset=9&visibility=Private&creator=3&sort=priority&sortDirection=desc&page=2&pageSize=50',
      ),
      3,
    )

    expect(state).toEqual({
      search: 'filter',
      type: 4,
      status: 'InProgress',
      priority: 'High',
      asset: 9,
      visibility: 'Private',
      mine: true,
      sort: 'priority',
      sortDirection: 'desc',
      page: 2,
      pageSize: 50,
    })
    expect(toListQuery(state, 3)).toEqual({
      search: 'filter',
      type: 4,
      status: 'InProgress',
      priority: 'High',
      asset: 9,
      visibility: 'Private',
      creator: 3,
      page: 2,
      pageSize: 50,
      sort: 'priority',
      sortDirection: 'desc',
    })
  })

  it('parses task dialog state separately from table state', () => {
    expect(parseMaintenanceDialogState(new URLSearchParams('newTask=true'))).toEqual({
      mode: 'create',
    })
    expect(parseMaintenanceDialogState(new URLSearchParams('taskId=7'))).toEqual({
      mode: 'edit',
      taskId: 7,
    })
    expect(parseMaintenanceDialogState(new URLSearchParams('taskId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates task request boundaries', () => {
    const request = maintenanceTaskRequestSchema.parse({
      title: '  Replace filter  ',
      maintenanceTypeId: 4,
      status: 'Pending',
      priority: 'Medium',
      dueDate: '2026-07-01',
      notes: '',
      assetId: 9,
      visibility: 'Public',
    })

    expect(request.title).toBe('Replace filter')
    expect(request.notes).toBeNull()
    expect(request.dueDate).toBe('2026-07-01')
    expect(request.assetId).toBe(9)
    expect(
      maintenanceTaskRequestSchema.safeParse({ ...request, status: 'Archived' })
        .success,
    ).toBe(false)
    expect(
      maintenanceTaskRequestSchema.safeParse({ ...request, priority: 'Urgent' })
        .success,
    ).toBe(false)
    expect(
      maintenanceTaskRequestSchema.safeParse({ ...request, dueDate: '01-07-2026' })
        .success,
    ).toBe(false)
  })
})
