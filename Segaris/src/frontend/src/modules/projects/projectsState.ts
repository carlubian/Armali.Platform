import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * URL-backed Projects workspace state. Selection is independent from dialogs:
 * tree clicks update the persistent detail pane, while create/edit/risk workflows
 * add their own temporary URL flags without replacing the selected node.
 */
export type ProjectsSelectionState =
  | { kind: 'none' }
  | { kind: 'program'; programId: number }
  | { kind: 'axis'; axisId: number }
  | { kind: 'project'; projectId: number }
  | { kind: 'activity'; activityId: number }

export type ProjectsDialogState =
  | { mode: 'closed' }
  | { mode: 'createItem'; axisId: number; itemMode: 'project' | 'activity' }
  | { mode: 'editProject'; projectId: number }
  | { mode: 'editActivity'; activityId: number }
  | { mode: 'projectRisks'; projectId: number }

const DIALOG_PARAMS = [
  'editProjectId',
  'editActivityId',
  'riskProjectId',
  'newItem',
  'newProject',
  'newActivity',
  // Legacy dialog params are cleared when a modern dialog action is applied.
  'projectId',
  'activityId',
  'risks',
] as const

function intOrNull(value: string | null): number | null {
  if (value == null) return null
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

/** Parses an `axis-{id}` parameter value into the referenced axis identifier. */
function axisRef(value: string | null): number | null {
  if (value == null) return null
  const match = /^axis-(\d+)$/.exec(value)
  return match == null ? null : intOrNull(match[1])
}

export function axisRefValue(axisId: number): string {
  return `axis-${axisId}`
}

export function selectionRefValue(
  selection: Exclude<ProjectsSelectionState, { kind: 'none' }>,
) {
  switch (selection.kind) {
    case 'program':
      return `program-${selection.programId}`
    case 'axis':
      return `axis-${selection.axisId}`
    case 'project':
      return `project-${selection.projectId}`
    case 'activity':
      return `activity-${selection.activityId}`
  }
}

export function parseProjectsSelectionState(
  params: URLSearchParams,
): ProjectsSelectionState {
  const selected = params.get('selected')
  if (selected != null) {
    const match = /^(program|axis|project|activity)-(\d+)$/.exec(selected)
    const id = match == null ? null : intOrNull(match[2])
    if (match != null && id != null) {
      switch (match[1]) {
        case 'program':
          return { kind: 'program', programId: id }
        case 'axis':
          return { kind: 'axis', axisId: id }
        case 'project':
          return { kind: 'project', projectId: id }
        case 'activity':
          return { kind: 'activity', activityId: id }
      }
    }
  }

  const legacyProjectId = intOrNull(params.get('projectId'))
  if (legacyProjectId != null) return { kind: 'project', projectId: legacyProjectId }

  const legacyActivityId = intOrNull(params.get('activityId'))
  if (legacyActivityId != null)
    return { kind: 'activity', activityId: legacyActivityId }

  return { kind: 'none' }
}

export function parseProjectsDialogState(params: URLSearchParams): ProjectsDialogState {
  const newItemAxis = axisRef(params.get('newItem'))
  if (newItemAxis != null) {
    return { mode: 'createItem', axisId: newItemAxis, itemMode: 'project' }
  }

  const newProjectAxis = axisRef(params.get('newProject'))
  if (newProjectAxis != null) {
    return { mode: 'createItem', axisId: newProjectAxis, itemMode: 'project' }
  }

  const newActivityAxis = axisRef(params.get('newActivity'))
  if (newActivityAxis != null) {
    return { mode: 'createItem', axisId: newActivityAxis, itemMode: 'activity' }
  }

  const projectId = intOrNull(params.get('editProjectId'))
  if (projectId != null) {
    return { mode: 'editProject', projectId }
  }

  const activityId = intOrNull(params.get('editActivityId'))
  if (activityId != null) return { mode: 'editActivity', activityId }

  const riskProjectId =
    intOrNull(params.get('riskProjectId')) ??
    (params.get('risks') === 'true' ? intOrNull(params.get('projectId')) : null)
  if (riskProjectId != null) return { mode: 'projectRisks', projectId: riskProjectId }

  return { mode: 'closed' }
}

export function useProjectsSelectionState() {
  const [searchParams, setSearchParams] = useSearchParams()
  const selection = useMemo(
    () => parseProjectsSelectionState(searchParams),
    [searchParams],
  )

  const apply = useCallback(
    (selection: ProjectsSelectionState) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        if (selection.kind === 'none') next.delete('selected')
        else next.set('selected', selectionRefValue(selection))
        return next
      })
    },
    [setSearchParams],
  )

  return {
    selection,
    selectProgram: (programId: number) => apply({ kind: 'program', programId }),
    selectAxis: (axisId: number) => apply({ kind: 'axis', axisId }),
    selectProject: (projectId: number) => apply({ kind: 'project', projectId }),
    selectActivity: (activityId: number) => apply({ kind: 'activity', activityId }),
    clearSelection: () => apply({ kind: 'none' }),
  }
}

export function useProjectsDialogState() {
  const [searchParams, setSearchParams] = useSearchParams()
  const dialog = useMemo(() => parseProjectsDialogState(searchParams), [searchParams])

  const apply = useCallback(
    (patch: Partial<Record<(typeof DIALOG_PARAMS)[number], string | null>>) => {
      setSearchParams((current) => {
        const next = new URLSearchParams(current)
        for (const key of DIALOG_PARAMS) next.delete(key)
        for (const [key, value] of Object.entries(patch)) {
          if (value != null) next.set(key, value)
        }
        return next
      })
    },
    [setSearchParams],
  )

  return {
    dialog,
    openCreateItem: (axisId: number) => apply({ newItem: axisRefValue(axisId) }),
    openProject: (projectId: number) => apply({ editProjectId: String(projectId) }),
    openProjectRisks: (projectId: number) =>
      apply({ riskProjectId: String(projectId) }),
    openActivity: (activityId: number) => apply({ editActivityId: String(activityId) }),
    closeDialog: () => apply({}),
  }
}
