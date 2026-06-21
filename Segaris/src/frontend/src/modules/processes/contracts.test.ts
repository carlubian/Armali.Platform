import { describe, expect, it } from 'vitest'

import {
  processPageSizes,
  processStatuses,
  processVisibilities,
  processesRoutePath,
  stepExecutionStates,
} from '@/app/api/processes'

import { processRequestSchema, processesKeys, updateStepListSchema } from './contracts'
import {
  defaultPageSize,
  defaultSort,
  defaultSortDirection,
  parseProcessesDialogState,
  parseProcessesState,
  toListQuery,
} from './processesState'

describe('processes contracts', () => {
  it('freezes route, status, and pagination constants', () => {
    expect(processesRoutePath).toBe('/processes')
    expect(processPageSizes).toEqual([10, 25, 50, 100])
    expect(processStatuses).toEqual([
      'NotStarted',
      'InProgress',
      'Completed',
      'Cancelled',
    ])
    expect(stepExecutionStates).toEqual(['Pending', 'Completed', 'Skipped'])
    expect(processVisibilities).toEqual(['Public', 'Private'])
    expect(defaultPageSize).toBe(25)
    expect(defaultSort).toBe('dueDate')
    expect(defaultSortDirection).toBe('asc')
  })

  it('freezes query keys for the list, categories, steps, and attachments', () => {
    expect(processesKeys.categories()).toEqual(['processes', 'categories'])
    expect(processesKeys.list({ page: 1 })).toEqual(['processes', 'list', { page: 1 }])
    expect(processesKeys.process(7)).toEqual(['processes', 7])
    expect(processesKeys.steps(7)).toEqual(['processes', 7, 'steps'])
    expect(processesKeys.attachments(7)).toEqual(['processes', 7, 'attachments'])
  })

  it('parses URL-backed table state with defaults and bounded values', () => {
    const state = parseProcessesState(
      new URLSearchParams(
        'search=passport&category=4&status=InProgress&visibility=Private&creator=3&sort=name&sortDirection=desc&page=2&pageSize=50',
      ),
      3,
    )

    expect(state).toEqual({
      search: 'passport',
      category: 4,
      status: 'InProgress',
      visibility: 'Private',
      mine: true,
      sort: 'name',
      sortDirection: 'desc',
      page: 2,
      pageSize: 50,
    })
    expect(toListQuery(state, 3)).toEqual({
      search: 'passport',
      category: 4,
      status: 'InProgress',
      visibility: 'Private',
      creator: 3,
      page: 2,
      pageSize: 50,
      sort: 'name',
      sortDirection: 'desc',
    })
  })

  it('parses dialog state, including the step-timeline popup, separately from table state', () => {
    expect(parseProcessesDialogState(new URLSearchParams('newProcess=true'))).toEqual({
      mode: 'create',
    })
    expect(parseProcessesDialogState(new URLSearchParams('processId=7'))).toEqual({
      mode: 'edit',
      processId: 7,
    })
    expect(
      parseProcessesDialogState(new URLSearchParams('processId=7&steps=true')),
    ).toEqual({ mode: 'steps', processId: 7 })
    expect(parseProcessesDialogState(new URLSearchParams('processId=0'))).toEqual({
      mode: 'closed',
    })
  })

  it('validates process request boundaries without accepting a status', () => {
    const request = processRequestSchema.parse({
      name: '  Renew passport  ',
      categoryId: 4,
      dueDate: '2026-07-01',
      notes: '',
      visibility: 'Public',
    })

    expect(request.name).toBe('Renew passport')
    expect(request.notes).toBeNull()
    expect(request.dueDate).toBe('2026-07-01')
    expect(
      processRequestSchema.safeParse({ ...request, visibility: 'Secret' }).success,
    ).toBe(false)
    expect(
      processRequestSchema.safeParse({ ...request, dueDate: '01-07-2026' }).success,
    ).toBe(false)
    expect(processRequestSchema.safeParse({ ...request, categoryId: 0 }).success).toBe(
      false,
    )
  })

  it('validates the step-list restructure request and preserves new-vs-existing identity', () => {
    const request = updateStepListSchema.parse({
      steps: [
        {
          id: 7,
          description: '  Gather documents  ',
          dueDate: '2026-06-01',
          notes: '',
          isOptional: false,
        },
        {
          id: null,
          description: 'Optional notarisation',
          dueDate: '',
          notes: null,
          isOptional: true,
        },
      ],
    })

    expect(request.steps[0].id).toBe(7)
    expect(request.steps[0].description).toBe('Gather documents')
    expect(request.steps[0].notes).toBeNull()
    expect(request.steps[1].id).toBeNull()
    expect(request.steps[1].dueDate).toBeNull()
    expect(
      updateStepListSchema.safeParse({ steps: [{ id: 1, isOptional: false }] }).success,
    ).toBe(false)
  })
})
