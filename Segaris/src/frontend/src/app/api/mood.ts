import { apiRequest } from './client'

// Fixed criteria vocabularies. Order mirrors the backend enum declaration order
// and the `mood/options` contract.
export type MoodEnergy = 'Low' | 'Medium' | 'High'
export type MoodAlignment = 'Negative' | 'Medium' | 'Positive'
export type MoodDirection = 'Harmony' | 'Defensive' | 'Offensive' | 'Stability'
export type MoodSource = 'Internal' | 'External'

export const moodEnergies = ['Low', 'Medium', 'High'] as const
export const moodAlignments = ['Negative', 'Medium', 'Positive'] as const
export const moodDirections = [
  'Harmony',
  'Defensive',
  'Offensive',
  'Stability',
] as const
export const moodSources = ['Internal', 'External'] as const

export type MoodDashboardScale = 'year' | 'semester' | 'quarter' | 'month'
export const moodDashboardScales = ['year', 'semester', 'quarter', 'month'] as const

export const moodScoreMin = 1 as const
export const moodScoreMax = 5 as const
export const moodNotesMaxLength = 1000 as const

export const moodRoutePath = '/mood' as const
export const moodLogRoutePath = '/mood/log' as const
export const moodDashboardRoutePath = '/mood/dashboard' as const

export interface MoodEntry {
  id: number
  entryDate: string
  score: number
  energy: MoodEnergy
  alignment: MoodAlignment
  direction: MoodDirection
  source: MoodSource
  derivedEmotion: string
  notes: string | null
  createdById: number
  createdByName: string
  createdAt: string
  updatedById: number | null
  updatedByName: string | null
  updatedAt: string | null
}

export interface MoodEntryRangeQuery {
  from: string
  to: string
}

/**
 * Average mood score for one civil date in a weekly log range. `averageScore` is
 * `null` for days the user did not log, so the chart renders a missing-data gap
 * rather than a zero.
 */
export interface MoodDailyAverage {
  entryDate: string
  averageScore: number | null
}

/**
 * Owner-only weekly log payload. `entries` preserve deterministic insertion order
 * within each day; `dailyAverages` carries one bucket per civil date in the range.
 */
export interface MoodEntryList {
  from: string
  to: string
  entries: MoodEntry[]
  dailyAverages: MoodDailyAverage[]
}

export interface CreateMoodEntryRequest {
  entryDate: string
  score: number
  energy: MoodEnergy
  alignment: MoodAlignment
  direction: MoodDirection
  source: MoodSource
  notes: string | null
}

export type UpdateMoodEntryRequest = CreateMoodEntryRequest

export interface MoodOptions {
  energies: MoodEnergy[]
  alignments: MoodAlignment[]
  directions: MoodDirection[]
  sources: MoodSource[]
  emotions: string[]
}

export interface MoodDerivedEmotionQuery {
  energy: MoodEnergy
  alignment: MoodAlignment
  direction: MoodDirection
  source: MoodSource
}

export interface MoodDerivedEmotion {
  derivedEmotion: string
}

// Dashboard chart-data skeletons. Wave 3 fills the aggregate computation; the
// response shape is frozen here so the Wave 5 charts and query keys are stable.
export interface MoodScoreStat {
  min: number | null
  average: number | null
  max: number | null
}

export interface MoodScoreByDay extends MoodScoreStat {
  /** ISO day of week, 1 = Monday through 7 = Sunday. */
  dayOfWeek: number
}

export interface MoodScoreByInterval extends MoodScoreStat {
  /** Month token (`2026-03`) for Year/Semester/Quarter, or a week token for Month. */
  interval: string
}

export interface MoodDistributionBucket {
  value: string
  count: number
}

export interface MoodCriteriaDistribution {
  energy: MoodDistributionBucket[]
  alignment: MoodDistributionBucket[]
  direction: MoodDistributionBucket[]
  source: MoodDistributionBucket[]
}

export interface MoodCriteriaEvolutionPoint {
  interval: string
  energy: Record<MoodEnergy, number>
  alignment: Record<MoodAlignment, number>
  direction: Record<MoodDirection, number>
  source: Record<MoodSource, number>
}

export interface MoodDashboardQuery {
  scale: MoodDashboardScale
  period: string
}

export interface MoodDashboard {
  scale: MoodDashboardScale
  period: string
  periodStart: string
  periodEnd: string
  previousPeriod: string
  nextPeriod: string
  scoreByDayOfWeek: MoodScoreByDay[]
  scoreByInterval: MoodScoreByInterval[]
  distribution: MoodCriteriaDistribution
  evolution: MoodCriteriaEvolutionPoint[]
}

interface MoodScoreByDayResponse {
  dayOfWeek: string
  minScore: number | null
  averageScore: number | null
  maxScore: number | null
}

interface MoodValueCountResponse {
  value: string
  count: number
}

interface MoodCriteriaDistributionResponse {
  energy: MoodValueCountResponse[]
  alignment: MoodValueCountResponse[]
  direction: MoodValueCountResponse[]
  source: MoodValueCountResponse[]
}

interface MoodBucketResponse {
  key: string
  minScore: number | null
  averageScore: number | null
  maxScore: number | null
  distribution: MoodCriteriaDistributionResponse
}

interface MoodDashboardResponse {
  scale: string
  period: string
  from: string
  to: string
  previousPeriod: string
  nextPeriod: string
  scoreByDayOfWeek: MoodScoreByDayResponse[]
  distribution: MoodCriteriaDistributionResponse
  buckets: MoodBucketResponse[]
}

function buildQuery(query: Record<string, string>): string {
  const parameters = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    const text = value.trim()
    if (text.length > 0) parameters.set(key, text)
  }
  const search = parameters.toString()
  return search ? `?${search}` : ''
}

const dayOfWeekToNumber: Record<string, number> = {
  Monday: 1,
  Tuesday: 2,
  Wednesday: 3,
  Thursday: 4,
  Friday: 5,
  Saturday: 6,
  Sunday: 7,
}

const scaleFromResponse: Record<string, MoodDashboardScale> = {
  Year: 'year',
  Semester: 'semester',
  Quarter: 'quarter',
  Month: 'month',
}

function countMap<T extends string>(
  values: readonly T[],
  buckets: readonly MoodValueCountResponse[],
): Record<T, number> {
  const counts = Object.fromEntries(values.map((value) => [value, 0])) as Record<
    T,
    number
  >
  for (const bucket of buckets) {
    if (values.includes(bucket.value as T)) counts[bucket.value as T] = bucket.count
  }
  return counts
}

function mapDistribution(
  distribution: MoodCriteriaDistributionResponse,
): MoodCriteriaDistribution {
  return {
    energy: distribution.energy,
    alignment: distribution.alignment,
    direction: distribution.direction,
    source: distribution.source,
  }
}

function mapDashboard(response: MoodDashboardResponse): MoodDashboard {
  return {
    scale: scaleFromResponse[response.scale] ?? 'year',
    period: response.period,
    periodStart: response.from,
    periodEnd: response.to,
    previousPeriod: response.previousPeriod,
    nextPeriod: response.nextPeriod,
    scoreByDayOfWeek: response.scoreByDayOfWeek.map((day) => ({
      dayOfWeek: dayOfWeekToNumber[day.dayOfWeek] ?? 1,
      min: day.minScore,
      average: day.averageScore,
      max: day.maxScore,
    })),
    scoreByInterval: response.buckets.map((bucket) => ({
      interval: bucket.key,
      min: bucket.minScore,
      average: bucket.averageScore,
      max: bucket.maxScore,
    })),
    distribution: mapDistribution(response.distribution),
    evolution: response.buckets.map((bucket) => ({
      interval: bucket.key,
      energy: countMap(moodEnergies, bucket.distribution.energy),
      alignment: countMap(moodAlignments, bucket.distribution.alignment),
      direction: countMap(moodDirections, bucket.distribution.direction),
      source: countMap(moodSources, bucket.distribution.source),
    })),
  }
}

export const moodApi = {
  options: (signal?: AbortSignal) =>
    apiRequest<MoodOptions>('/api/mood/options', { signal }),
  derivedEmotion: (query: MoodDerivedEmotionQuery, signal?: AbortSignal) =>
    apiRequest<MoodDerivedEmotion>(
      `/api/mood/derived-emotion${buildQuery({
        energy: query.energy,
        alignment: query.alignment,
        direction: query.direction,
        source: query.source,
      })}`,
      { signal },
    ),
  listEntries: (query: MoodEntryRangeQuery, signal?: AbortSignal) =>
    apiRequest<MoodEntryList>(
      `/api/mood/entries${buildQuery({ from: query.from, to: query.to })}`,
      { signal },
    ),
  getEntry: (entryId: number, signal?: AbortSignal) =>
    apiRequest<MoodEntry>(`/api/mood/entries/${entryId}`, { signal }),
  createEntry: (request: CreateMoodEntryRequest, signal?: AbortSignal) =>
    apiRequest<MoodEntry>('/api/mood/entries', {
      method: 'POST',
      body: JSON.stringify(request),
      signal,
    }),
  updateEntry: (
    entryId: number,
    request: UpdateMoodEntryRequest,
    signal?: AbortSignal,
  ) =>
    apiRequest<MoodEntry>(`/api/mood/entries/${entryId}`, {
      method: 'PUT',
      body: JSON.stringify(request),
      signal,
    }),
  deleteEntry: (entryId: number, signal?: AbortSignal) =>
    apiRequest<void>(`/api/mood/entries/${entryId}`, { method: 'DELETE', signal }),
  dashboard: async (query: MoodDashboardQuery, signal?: AbortSignal) =>
    mapDashboard(
      await apiRequest<MoodDashboardResponse>(
        `/api/mood/dashboard${buildQuery({ scale: query.scale, period: query.period })}`,
        { signal },
      ),
    ),
}
