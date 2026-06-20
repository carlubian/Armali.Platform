import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * URL-backed dialog state for the Projects page. Programs and axes are read-only
 * structure in the tree; only projects and activities are created and edited, through
 * the established Segaris URL-aware popup pattern, so the tree's in-memory expansion and
 * selection survive dialog open and close without a reload. A project's risk table opens
 * as a further popup, tracked by the `risks` flag.
 */
export type ProjectsDialogState =
  | { mode: 'closed' }
  | { mode: 'createProject'; axisId: number }
  | { mode: 'createActivity'; axisId: number }
  | { mode: 'editProject'; projectId: number; risks: boolean }
  | { mode: 'editActivity'; activityId: number }

const DIALOG_PARAMS = [
  'projectId',
  'activityId',
  'newProject',
  'newActivity',
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

export function parseProjectsDialogState(params: URLSearchParams): ProjectsDialogState {
  const newProjectAxis = axisRef(params.get('newProject'))
  if (newProjectAxis != null) return { mode: 'createProject', axisId: newProjectAxis }

  const newActivityAxis = axisRef(params.get('newActivity'))
  if (newActivityAxis != null) {
    return { mode: 'createActivity', axisId: newActivityAxis }
  }

  const projectId = intOrNull(params.get('projectId'))
  if (projectId != null) {
    return { mode: 'editProject', projectId, risks: params.get('risks') === 'true' }
  }

  const activityId = intOrNull(params.get('activityId'))
  if (activityId != null) return { mode: 'editActivity', activityId }

  return { mode: 'closed' }
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
    openCreateProject: (axisId: number) => apply({ newProject: axisRefValue(axisId) }),
    openCreateActivity: (axisId: number) =>
      apply({ newActivity: axisRefValue(axisId) }),
    openProject: (projectId: number) => apply({ projectId: String(projectId) }),
    openProjectRisks: (projectId: number) =>
      apply({ projectId: String(projectId), risks: 'true' }),
    closeProjectRisks: (projectId: number) => apply({ projectId: String(projectId) }),
    openActivity: (activityId: number) => apply({ activityId: String(activityId) }),
    closeDialog: () => apply({}),
  }
}
