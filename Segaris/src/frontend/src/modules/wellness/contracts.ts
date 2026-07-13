import { z } from 'zod'

import {
  wellnessCategories,
  wellnessTaskNameMaxLength,
  type WellnessCategory,
  type WellnessDaysQuery,
  type WellnessTaskRequest,
} from '@/app/api/wellness'

export const wellnessKeys = {
  all: ['wellness'] as const,
  today: () => [...wellnessKeys.all, 'today'] as const,
  tasks: () => [...wellnessKeys.all, 'tasks'] as const,
  days: (query: WellnessDaysQuery) => [...wellnessKeys.all, 'days', query] as const,
}

const categoryValues = wellnessCategories as [WellnessCategory, ...WellnessCategory[]]

export const wellnessTaskRequestSchema = z.object({
  name: z.string().trim().min(1).max(wellnessTaskNameMaxLength),
  category: z.enum(categoryValues),
}) satisfies z.ZodType<WellnessTaskRequest>
