import { z } from 'zod'

import type {
  AxisRequest,
  CreateActivityRequest,
  CreateProjectRequest,
  ProgramRequest,
  ProjectRiskRequest,
} from '@/app/api/projects'
import {
  projectStatuses,
  projectVisibilities,
  riskFactorRange,
  structuralCodeLength,
} from '@/app/api/projects'

export const projectsKeys = {
  all: ['projects'] as const,
  tree: () => [...projectsKeys.all, 'tree'] as const,
  programs: () => [...projectsKeys.tree(), 'programs'] as const,
  axes: (programId: number) => [...projectsKeys.programs(), programId, 'axes'] as const,
  items: (axisId: number) => [...projectsKeys.tree(), 'axes', axisId, 'items'] as const,
  projects: () => [...projectsKeys.all, 'projects'] as const,
  project: (projectId: number) => [...projectsKeys.projects(), projectId] as const,
  projectRisks: (projectId: number) =>
    [...projectsKeys.project(projectId), 'risks'] as const,
  projectAttachments: (projectId: number) =>
    [...projectsKeys.project(projectId), 'attachments'] as const,
  activities: () => [...projectsKeys.all, 'activities'] as const,
  activity: (activityId: number) => [...projectsKeys.activities(), activityId] as const,
  structure: () => [...projectsKeys.all, 'structure'] as const,
  structurePrograms: () => [...projectsKeys.structure(), 'programs'] as const,
  structureAxes: () => [...projectsKeys.structure(), 'axes'] as const,
}

const name = z.string().trim().min(1).max(200)
const status = z.enum(projectStatuses)
const visibility = z.enum(projectVisibilities)
const structuralCode = z
  .string()
  .trim()
  .toUpperCase()
  .regex(new RegExp(`^[A-Z]{${structuralCodeLength}}$`), {
    message: `Code must be exactly ${structuralCodeLength} uppercase letters.`,
  })
const riskFactor = z.number().int().min(riskFactorRange.min).max(riskFactorRange.max)
const positiveId = z.number().int().positive()

export const projectRequestSchema = z.object({
  axisId: positiveId,
  name,
  status,
  visibility,
}) satisfies z.ZodType<CreateProjectRequest>

export const activityRequestSchema = z.object({
  axisId: positiveId,
  name,
  status,
  visibility,
}) satisfies z.ZodType<CreateActivityRequest>

export const projectRiskRequestSchema = z.object({
  description: z.string().trim().min(1).max(1000),
  probability: riskFactor,
  impact: riskFactor,
  mitigation: riskFactor,
}) satisfies z.ZodType<ProjectRiskRequest>

export const programRequestSchema = z.object({
  name,
  code: structuralCode,
}) satisfies z.ZodType<ProgramRequest>

export const axisRequestSchema = z.object({
  name,
  code: structuralCode,
  programId: positiveId,
}) satisfies z.ZodType<AxisRequest>
