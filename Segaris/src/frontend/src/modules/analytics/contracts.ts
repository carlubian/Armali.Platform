import { z } from 'zod'

import {
  analyticsMaximumYear,
  analyticsMinimumYear,
  analyticsTabs,
  type AnalyticsTab,
} from '@/app/api/analytics'

export const analyticsKeys = {
  all: ['analytics'] as const,
  year: (year: number) => [...analyticsKeys.all, year] as const,
  tab: (year: number, tab: AnalyticsTab) => [...analyticsKeys.year(year), tab] as const,
}

export const analyticsYearSchema = z
  .number()
  .int()
  .min(analyticsMinimumYear)
  .max(analyticsMaximumYear)

export const analyticsTabSchema = z.enum(analyticsTabs)
