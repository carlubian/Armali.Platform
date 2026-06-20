import { describe, expect, it } from 'vitest'

import {
  projectStatuses,
  projectVisibilities,
  projectsRoutePath,
  riskFactorRange,
  structuralCodeLength,
} from '@/app/api/projects'

import {
  activityRequestSchema,
  axisRequestSchema,
  programRequestSchema,
  projectRequestSchema,
  projectRiskRequestSchema,
  projectsKeys,
} from './contracts'
import {
  axisRefValue,
  parseProjectsDialogState,
  parseProjectsSelectionState,
  selectionRefValue,
} from './projectsState'

describe('projects contracts', () => {
  it('freezes route and vocabulary constants', () => {
    expect(projectsRoutePath).toBe('/projects')
    expect(projectStatuses).toEqual([
      'Planning',
      'Active',
      'Completed',
      'OnHold',
      'Cancelled',
    ])
    expect(projectVisibilities).toEqual(['Public', 'Private'])
    expect(structuralCodeLength).toBe(4)
    expect(riskFactorRange).toEqual({ min: 1, max: 5 })
  })

  it('freezes query keys for the tree, items, risks, and attachments', () => {
    expect(projectsKeys.programs()).toEqual(['projects', 'tree', 'programs'])
    expect(projectsKeys.axes(3)).toEqual(['projects', 'tree', 'programs', 3, 'axes'])
    expect(projectsKeys.items(5)).toEqual(['projects', 'tree', 'axes', 5, 'items'])
    expect(projectsKeys.project(7)).toEqual(['projects', 'projects', 7])
    expect(projectsKeys.projectRisks(7)).toEqual(['projects', 'projects', 7, 'risks'])
    expect(projectsKeys.projectAttachments(7)).toEqual([
      'projects',
      'projects',
      7,
      'attachments',
    ])
    expect(projectsKeys.activity(9)).toEqual(['projects', 'activities', 9])
  })

  it('validates project and activity request boundaries', () => {
    const project = projectRequestSchema.parse({
      axisId: 5,
      name: '  Cellar renovation  ',
      status: 'Planning',
      visibility: 'Public',
    })
    expect(project.name).toBe('Cellar renovation')

    expect(
      activityRequestSchema.safeParse({
        axisId: 5,
        name: 'Tidy garage',
        status: 'Active',
        visibility: 'Private',
      }).success,
    ).toBe(true)

    expect(
      projectRequestSchema.safeParse({ ...project, status: 'Archived' }).success,
    ).toBe(false)
    expect(projectRequestSchema.safeParse({ ...project, name: '   ' }).success).toBe(
      false,
    )
    expect(projectRequestSchema.safeParse({ ...project, axisId: 0 }).success).toBe(
      false,
    )
  })

  it('validates risk factors within the inclusive 1-5 range', () => {
    const risk = projectRiskRequestSchema.parse({
      description: 'Supplier delay',
      probability: 3,
      impact: 4,
      mitigation: 5,
    })
    expect(risk.probability).toBe(3)

    expect(projectRiskRequestSchema.safeParse({ ...risk, impact: 0 }).success).toBe(
      false,
    )
    expect(projectRiskRequestSchema.safeParse({ ...risk, mitigation: 6 }).success).toBe(
      false,
    )
    expect(
      projectRiskRequestSchema.safeParse({ ...risk, probability: 2.5 }).success,
    ).toBe(false)
    expect(
      projectRiskRequestSchema.safeParse({ ...risk, description: '' }).success,
    ).toBe(false)
  })

  it('validates four-uppercase-letter program and axis codes', () => {
    const program = programRequestSchema.parse({ name: 'Infrastructure', code: 'infr' })
    expect(program.code).toBe('INFR')

    expect(
      axisRequestSchema.safeParse({ name: 'Web', code: 'WEBS', programId: 1 }).success,
    ).toBe(true)
    expect(programRequestSchema.safeParse({ name: 'X', code: 'ABC' }).success).toBe(
      false,
    )
    expect(programRequestSchema.safeParse({ name: 'X', code: 'ABCDE' }).success).toBe(
      false,
    )
    expect(programRequestSchema.safeParse({ name: 'X', code: 'AB1C' }).success).toBe(
      false,
    )
  })

  it('parses URL-backed selection independently from dialog state', () => {
    expect(parseProjectsSelectionState(new URLSearchParams())).toEqual({
      kind: 'none',
    })
    expect(
      parseProjectsSelectionState(
        new URLSearchParams(
          `selected=${selectionRefValue({ kind: 'program', programId: 123 })}`,
        ),
      ),
    ).toEqual({ kind: 'program', programId: 123 })
    expect(
      parseProjectsSelectionState(new URLSearchParams('selected=axis-456')),
    ).toEqual({
      kind: 'axis',
      axisId: 456,
    })
    expect(
      parseProjectsSelectionState(
        new URLSearchParams('selected=project-789&editProjectId=789'),
      ),
    ).toEqual({ kind: 'project', projectId: 789 })
    expect(
      parseProjectsSelectionState(
        new URLSearchParams('selected=activity-321&riskProjectId=789'),
      ),
    ).toEqual({ kind: 'activity', activityId: 321 })
    expect(parseProjectsSelectionState(new URLSearchParams('projectId=123'))).toEqual({
      kind: 'project',
      projectId: 123,
    })
  })

  it('parses URL-backed dialog state for creates, explicit edits, and risks', () => {
    expect(parseProjectsDialogState(new URLSearchParams())).toEqual({
      mode: 'closed',
    })
    expect(
      parseProjectsDialogState(new URLSearchParams(`newItem=${axisRefValue(789)}`)),
    ).toEqual({ mode: 'createItem', axisId: 789, itemMode: 'project' })
    expect(
      parseProjectsDialogState(new URLSearchParams(`newProject=${axisRefValue(789)}`)),
    ).toEqual({ mode: 'createItem', axisId: 789, itemMode: 'project' })
    expect(
      parseProjectsDialogState(new URLSearchParams('newActivity=axis-789')),
    ).toEqual({ mode: 'createItem', axisId: 789, itemMode: 'activity' })
    expect(parseProjectsDialogState(new URLSearchParams('editProjectId=123'))).toEqual({
      mode: 'editProject',
      projectId: 123,
    })
    expect(parseProjectsDialogState(new URLSearchParams('riskProjectId=123'))).toEqual({
      mode: 'projectRisks',
      projectId: 123,
    })
    expect(
      parseProjectsDialogState(new URLSearchParams('projectId=123&risks=true')),
    ).toEqual({ mode: 'projectRisks', projectId: 123 })
    expect(parseProjectsDialogState(new URLSearchParams('editActivityId=456'))).toEqual(
      {
        mode: 'editActivity',
        activityId: 456,
      },
    )
    expect(parseProjectsDialogState(new URLSearchParams('editProjectId=0'))).toEqual({
      mode: 'closed',
    })
  })
})
