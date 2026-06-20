import { apiRequest } from './client'

/**
 * Frozen Projects API contracts and client. The module organises work into a
 * `Program` → `Axis` → (`Project` | `Activity`) tree. Programs and axes are
 * module-owned structural nodes managed through Configuration; projects and
 * activities are managed from the Projects page. The unified identifier is computed
 * by the backend and is never built or persisted on the client.
 */

export type ProjectStatus = 'Planning' | 'Active' | 'Completed' | 'OnHold' | 'Cancelled'
export type ProjectVisibility = 'Public' | 'Private'
export type ProjectItemKind = 'Project' | 'Activity'
export type RiskBand = 'Low' | 'Medium' | 'High'

export const projectStatuses: readonly ProjectStatus[] = [
  'Planning',
  'Active',
  'Completed',
  'OnHold',
  'Cancelled',
]
export const projectVisibilities: readonly ProjectVisibility[] = ['Public', 'Private']

export const projectsRoutePath = '/projects' as const

/** Exact length of a program or axis code (four uppercase ASCII letters). */
export const structuralCodeLength = 4 as const
/** Inclusive bounds of a risk probability, impact, or mitigation factor. */
export const riskFactorRange = { min: 1, max: 5 } as const

export interface ProgramNode {
  id: number
  code: string
  name: string
}

export interface AxisNode {
  id: number
  code: string
  name: string
  programId: number
}

export interface ProjectRiskBandSummary {
  low: number
  medium: number
  high: number
}

export interface ProjectTreeItem {
  id: number
  kind: ProjectItemKind
  number: number
  identifier: string
  name: string
  status: ProjectStatus
  visibility: ProjectVisibility
  riskSummary: ProjectRiskBandSummary | null
}

export interface ProjectAttachment {
  id: string
  fileName: string
  contentType: string
  size: number
  createdById: number
  createdAt: string
}

export interface Project {
  id: number
  number: number
  identifier: string
  name: string
  status: ProjectStatus
  visibility: ProjectVisibility
  axisId: number
  riskSummary: ProjectRiskBandSummary
  attachments: ProjectAttachment[]
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface Activity {
  id: number
  number: number
  identifier: string
  name: string
  status: ProjectStatus
  visibility: ProjectVisibility
  axisId: number
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface ProjectRisk {
  id: number
  description: string
  probability: number
  impact: number
  mitigation: number
  score: number
  band: RiskBand
}

export interface StructuralNodeDeletionImpact {
  childCount: number
  hasCompatibleTarget: boolean
}

export interface CreateProjectRequest {
  axisId: number
  name: string
  status: ProjectStatus
  visibility: ProjectVisibility
}

export type UpdateProjectRequest = CreateProjectRequest

export interface CreateActivityRequest {
  axisId: number
  name: string
  status: ProjectStatus
  visibility: ProjectVisibility
}

export type UpdateActivityRequest = CreateActivityRequest

export interface ProjectRiskRequest {
  description: string
  probability: number
  impact: number
  mitigation: number
}

export interface ProgramRequest {
  name: string
  code: string
}

export interface AxisRequest {
  name: string
  code: string
  programId: number
}

export interface StructuralNodeReassignmentRequest {
  targetNodeId: number
}

export const projectsApi = {
  // Lazily expanded tree reads.
  programs: (signal?: AbortSignal) =>
    apiRequest<ProgramNode[]>('/api/projects/tree/programs', { signal }),
  axes: (programId: number, signal?: AbortSignal) =>
    apiRequest<AxisNode[]>(`/api/projects/tree/programs/${programId}/axes`, { signal }),
  items: (axisId: number, signal?: AbortSignal) =>
    apiRequest<ProjectTreeItem[]>(`/api/projects/tree/axes/${axisId}/items`, {
      signal,
    }),

  // Project lifecycle.
  getProject: (projectId: number, signal?: AbortSignal) =>
    apiRequest<Project>(`/api/projects/projects/${projectId}`, { signal }),
  createProject: (request: CreateProjectRequest, signal?: AbortSignal) =>
    apiRequest<Project>('/api/projects/projects', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateProject: (
    projectId: number,
    request: UpdateProjectRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Project>(`/api/projects/projects/${projectId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteProject: (projectId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/projects/projects/${projectId}`, {
      method: 'DELETE',
      signal,
    }),

  // Activity lifecycle.
  getActivity: (activityId: number, signal?: AbortSignal) =>
    apiRequest<Activity>(`/api/projects/activities/${activityId}`, { signal }),
  createActivity: (request: CreateActivityRequest, signal?: AbortSignal) =>
    apiRequest<Activity>('/api/projects/activities', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateActivity: (
    activityId: number,
    request: UpdateActivityRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<Activity>(`/api/projects/activities/${activityId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteActivity: (activityId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/projects/activities/${activityId}`, {
      method: 'DELETE',
      signal,
    }),

  // Project risks.
  listRisks: (projectId: number, signal?: AbortSignal) =>
    apiRequest<ProjectRisk[]>(`/api/projects/projects/${projectId}/risks`, {
      signal,
    }),
  createRisk: (projectId: number, request: ProjectRiskRequest, signal?: AbortSignal) =>
    apiRequest<ProjectRisk>(`/api/projects/projects/${projectId}/risks`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateRisk: (
    projectId: number,
    riskId: number,
    request: ProjectRiskRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<ProjectRisk>(`/api/projects/projects/${projectId}/risks/${riskId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteRisk: (projectId: number, riskId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/projects/projects/${projectId}/risks/${riskId}`, {
      method: 'DELETE',
      signal,
    }),

  // Project attachments (no primary image); activities have none.
  listAttachments: (projectId: number, signal?: AbortSignal) =>
    apiRequest<ProjectAttachment[]>(`/api/projects/projects/${projectId}/attachments`, {
      signal,
    }),
  uploadAttachment: (projectId: number, file: File, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file)
    return apiRequest<ProjectAttachment>(
      `/api/projects/projects/${projectId}/attachments`,
      { method: 'POST', body, signal, timeoutMs: 60_000 },
    )
  },
  attachmentDownloadUrl: (projectId: number, attachmentId: string) =>
    `/api/projects/projects/${projectId}/attachments/${attachmentId}`,
  deleteAttachment: (projectId: number, attachmentId: string, signal?: AbortSignal) =>
    apiRequest<void>(
      `/api/projects/projects/${projectId}/attachments/${attachmentId}`,
      { method: 'DELETE', signal },
    ),
}

/**
 * Module-owned program and axis management, presented through Configuration.
 * Deleting a non-empty node requires reassigning every child to a single
 * compatible target, so these clients diverge from the shared flat-catalogue
 * client.
 */
export const projectsStructureApi = {
  listPrograms: (signal?: AbortSignal) =>
    apiRequest<ProgramNode[]>('/api/projects/programs', { signal }),
  createProgram: (request: ProgramRequest, signal?: AbortSignal) =>
    apiRequest<ProgramNode>('/api/projects/programs', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateProgram: (programId: number, request: ProgramRequest, signal?: AbortSignal) =>
    apiRequest<ProgramNode>(`/api/projects/programs/${programId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  programDeletionImpact: (programId: number, signal?: AbortSignal) =>
    apiRequest<StructuralNodeDeletionImpact>(
      `/api/projects/programs/${programId}/deletion-impact`,
      { signal },
    ),
  deleteProgram: (programId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/projects/programs/${programId}`, {
      method: 'DELETE',
      signal,
    }),
  reassignAndDeleteProgram: (
    programId: number,
    request: StructuralNodeReassignmentRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/projects/programs/${programId}/reassign-and-delete`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),

  listAxes: (signal?: AbortSignal) =>
    apiRequest<AxisNode[]>('/api/projects/axes', { signal }),
  createAxis: (request: AxisRequest, signal?: AbortSignal) =>
    apiRequest<AxisNode>('/api/projects/axes', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateAxis: (axisId: number, request: AxisRequest, signal?: AbortSignal) =>
    apiRequest<AxisNode>(`/api/projects/axes/${axisId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  axisDeletionImpact: (axisId: number, signal?: AbortSignal) =>
    apiRequest<StructuralNodeDeletionImpact>(
      `/api/projects/axes/${axisId}/deletion-impact`,
      { signal },
    ),
  deleteAxis: (axisId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/projects/axes/${axisId}`, {
      method: 'DELETE',
      signal,
    }),
  reassignAndDeleteAxis: (
    axisId: number,
    request: StructuralNodeReassignmentRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<void>(`/api/projects/axes/${axisId}/reassign-and-delete`, {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
}
