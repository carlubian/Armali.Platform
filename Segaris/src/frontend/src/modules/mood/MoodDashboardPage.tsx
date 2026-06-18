import { ChevronLeft, ChevronRight, RotateCcw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'

import type {
  MoodAlignment,
  MoodCriteriaEvolutionPoint,
  MoodDashboardScale,
  MoodDirection,
  MoodDistributionBucket,
  MoodEnergy,
  MoodScoreByDay,
  MoodScoreByInterval,
  MoodScoreStat,
  MoodSource,
} from '@/app/api/mood'
import {
  moodAlignments,
  moodDashboardScales,
  moodDirections,
  moodEnergies,
  moodSources,
} from '@/app/api/mood'
import { Button, Spinner } from '@/components/ui'

import {
  alignmentTone,
  directionTone,
  energyTone,
  moodToneVars,
  scoreColor,
  sourceTone,
  type MoodTone,
} from './criteria'
import {
  currentPeriod,
  nextPeriod,
  parseDashboardState,
  previousPeriod,
} from './dashboardState'
import { householdToday } from './entryForm'
import { MoodShell } from './MoodShell'
import { useMoodDashboard } from './queries'

type CriteriaKey = 'energy' | 'alignment' | 'direction' | 'source'

const dayOrder = [1, 2, 3, 4, 5, 6, 7] as const
const criteriaValues = {
  energy: moodEnergies,
  alignment: moodAlignments,
  direction: moodDirections,
  source: moodSources,
} as const

const criteriaTones = {
  energy: energyTone,
  alignment: alignmentTone,
  direction: directionTone,
  source: sourceTone,
} as const

function setDashboardParams(
  setSearchParams: ReturnType<typeof useSearchParams>[1],
  scale: MoodDashboardScale,
  period: string,
) {
  setSearchParams({ scale, period }, { replace: false })
}

function formatIsoDate(iso: string, language: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return new Intl.DateTimeFormat(language, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, month - 1, day)))
}

function periodTitle(
  scale: MoodDashboardScale,
  period: string,
  language: string,
): string {
  if (scale === 'year') return period
  if (scale === 'semester') return period.replace('-S', ' · H')
  if (scale === 'quarter') return period.replace('-Q', ' · Q')

  const [year, month] = period.split('-').map(Number)
  return new Intl.DateTimeFormat(language, {
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, month - 1, 1)))
}

function intervalLabel(interval: string, language: string): string {
  if (/^\d{4}-\d{2}$/.test(interval)) {
    const [year, month] = interval.split('-').map(Number)
    return new Intl.DateTimeFormat(language, {
      month: 'short',
      timeZone: 'UTC',
    }).format(new Date(Date.UTC(year, month - 1, 1)))
  }

  const week = /W(\d{1,2})/.exec(interval)
  if (week != null) return `W${week[1].padStart(2, '0')}`
  return interval
}

function statHasData(stat: MoodScoreStat): boolean {
  return stat.min != null || stat.average != null || stat.max != null
}

function formatStat(value: number | null): string {
  return value == null ? '·' : value.toFixed(1)
}

function average(values: number[]): number | null {
  if (values.length === 0) return null
  return values.reduce((total, value) => total + value, 0) / values.length
}

function countEntries(buckets: readonly MoodDistributionBucket[]): number {
  return buckets.reduce((total, bucket) => total + bucket.count, 0)
}

export function MoodDashboardPage() {
  const { t, i18n } = useTranslation('mood')
  const [searchParams, setSearchParams] = useSearchParams()
  const today = householdToday()
  const state = parseDashboardState(searchParams, today)
  const dashboardQuery = useMoodDashboard(state)
  const dashboard = dashboardQuery.data

  const entryCount = countEntries(dashboard?.distribution.energy ?? [])
  const hasScoreData =
    dashboard?.scoreByDayOfWeek.some(statHasData) === true ||
    dashboard?.scoreByInterval.some(statHasData) === true
  const hasEvolution = dashboard?.evolution.some((point) =>
    (Object.keys(criteriaValues) as CriteriaKey[]).some((key) =>
      Object.values(point[key]).some((count) => count > 0),
    ),
  )
  const isEmpty =
    dashboard != null && entryCount === 0 && !hasScoreData && !hasEvolution

  const periodRange =
    dashboard == null
      ? ''
      : t('dashboard.period.range', {
          start: formatIsoDate(dashboard.periodStart, i18n.language),
          end: formatIsoDate(dashboard.periodEnd, i18n.language),
        })

  const controls = (
    <div className="mood-controls mood-controls--dashboard">
      <div className="mood-seg" role="group" aria-label={t('dashboard.scale.label')}>
        {moodDashboardScales.map((scale) => (
          <button
            key={scale}
            type="button"
            className={['mood-seg__btn', scale === state.scale ? 'is-active' : ''].join(
              ' ',
            )}
            aria-pressed={scale === state.scale}
            onClick={() =>
              setDashboardParams(setSearchParams, scale, currentPeriod(scale, today))
            }
          >
            {t(`dashboard.scale.${scale}`)}
          </button>
        ))}
      </div>
      <div className="mood-nav" role="group" aria-label={t('dashboard.period.label')}>
        <button
          className="mood-nav__btn"
          type="button"
          aria-label={t('dashboard.period.previous')}
          onClick={() => {
            const period = previousPeriod(state.scale, state.period)
            if (period != null) setDashboardParams(setSearchParams, state.scale, period)
          }}
        >
          <ChevronLeft size={18} aria-hidden="true" />
        </button>
        <span className="mood-nav__label mood-nav__label--stacked">
          {periodTitle(state.scale, state.period, i18n.language)}
          <small>{periodRange || t(`dashboard.scale.${state.scale}`)}</small>
        </span>
        <button
          className="mood-nav__btn"
          type="button"
          aria-label={t('dashboard.period.next')}
          onClick={() => {
            const period = nextPeriod(state.scale, state.period)
            if (period != null) setDashboardParams(setSearchParams, state.scale, period)
          }}
        >
          <ChevronRight size={18} aria-hidden="true" />
        </button>
      </div>
      <Button
        variant="outline"
        size="sm"
        iconLeft={<RotateCcw size={16} aria-hidden="true" />}
        onClick={() =>
          setDashboardParams(
            setSearchParams,
            state.scale,
            currentPeriod(state.scale, today),
          )
        }
        disabled={state.period === currentPeriod(state.scale, today)}
      >
        {t('dashboard.period.current')}
      </Button>
    </div>
  )

  return (
    <MoodShell
      eyebrow={t('dashboard.eyebrow')}
      title={t('dashboard.title')}
      description={t('dashboard.description')}
      controls={controls}
    >
      {dashboardQuery.isLoading ? (
        <div className="seg-mood__loading">
          <Spinner size={22} label={t('dashboard.states.loading')} />
          <span>{t('dashboard.states.loading')}</span>
        </div>
      ) : dashboardQuery.isError ? (
        <p className="seg-mood__error" role="alert">
          {t('dashboard.states.loadError')}
        </p>
      ) : dashboard == null || isEmpty ? (
        <div className="mood-card">
          <div className="mood-emptynote">{t('dashboard.states.empty')}</div>
        </div>
      ) : (
        <div className="mood-dash">
          <ScoreSummaryCard
            dayStats={dashboard.scoreByDayOfWeek}
            intervalStats={dashboard.scoreByInterval}
            entryCount={entryCount}
            period={periodTitle(state.scale, state.period, i18n.language)}
          />
          <div className="mood-distgrid">
            <DistributionCard
              criterion="energy"
              buckets={dashboard.distribution.energy}
            />
            <DistributionCard
              criterion="alignment"
              buckets={dashboard.distribution.alignment}
            />
            <DistributionCard
              criterion="direction"
              buckets={dashboard.distribution.direction}
            />
            <DistributionCard
              criterion="source"
              buckets={dashboard.distribution.source}
            />
          </div>
          <div className="mood-grid mood-grid--two">
            <ScoreRangeCard
              title={t('dashboard.charts.dayOfWeek.title')}
              subtitle={t('dashboard.charts.dayOfWeek.subtitle')}
              label={t('dashboard.charts.dayOfWeek.aria')}
              points={dayOrder.map((day) => ({
                key: String(day),
                label: t(`dashboard.days.${day}`),
                ...emptyStat(),
                ...dashboard.scoreByDayOfWeek.find((row) => row.dayOfWeek === day),
              }))}
            />
            <ScoreRangeCard
              title={t('dashboard.charts.interval.title')}
              subtitle={t(`dashboard.charts.interval.subtitle.${state.scale}`)}
              label={t('dashboard.charts.interval.aria')}
              points={dashboard.scoreByInterval.map((row) => ({
                key: row.interval,
                label: intervalLabel(row.interval, i18n.language),
                ...row,
              }))}
            />
          </div>
          <div className="mood-grid mood-grid--two">
            <EvolutionCard criterion="energy" points={dashboard.evolution} />
            <EvolutionCard criterion="alignment" points={dashboard.evolution} />
            <EvolutionCard criterion="direction" points={dashboard.evolution} />
            <EvolutionCard criterion="source" points={dashboard.evolution} />
          </div>
        </div>
      )}
    </MoodShell>
  )
}

function emptyStat(): MoodScoreStat {
  return { min: null, average: null, max: null }
}

function ScoreSummaryCard({
  dayStats,
  intervalStats,
  entryCount,
  period,
}: {
  dayStats: MoodScoreByDay[]
  intervalStats: MoodScoreByInterval[]
  entryCount: number
  period: string
}) {
  const { t } = useTranslation('mood')
  const stats = [...dayStats, ...intervalStats].filter(statHasData)
  const low = stats
    .map((stat) => stat.min)
    .filter((value): value is number => value != null)
  const high = stats
    .map((stat) => stat.max)
    .filter((value): value is number => value != null)
  const avg = average(
    intervalStats
      .map((stat) => stat.average)
      .filter((value): value is number => value != null),
  )

  return (
    <div className="mood-card mood-card--summary">
      <div className="mood-chartcard__lead">
        <div className="armali-eyebrow">{t('dashboard.summary.eyebrow')}</div>
        <div className="mood-chartcard__big">{formatStat(avg)}</div>
        <small>
          {t('dashboard.summary.meta', {
            count: entryCount,
            low: low.length > 0 ? Math.min(...low).toFixed(0) : '·',
            high: high.length > 0 ? Math.max(...high).toFixed(0) : '·',
          })}
        </small>
      </div>
      <ScoreRangeChart
        label={t('dashboard.charts.dayOfWeek.aria')}
        points={dayOrder.map((day) => ({
          key: String(day),
          label: t(`dashboard.days.${day}`),
          ...emptyStat(),
          ...dayStats.find((row) => row.dayOfWeek === day),
        }))}
        compact
      />
      <span className="mood-card__sub">{period}</span>
    </div>
  )
}

function ScoreRangeCard({
  title,
  subtitle,
  label,
  points,
}: {
  title: string
  subtitle: string
  label: string
  points: Array<MoodScoreStat & { key: string; label: string }>
}) {
  return (
    <div className="mood-card">
      <div className="mood-card__head">
        <span className="mood-card__title">{title}</span>
        <span className="mood-card__sub">{subtitle}</span>
      </div>
      <ScoreRangeChart label={label} points={points} />
    </div>
  )
}

function ScoreRangeChart({
  label,
  points,
  compact = false,
}: {
  label: string
  points: Array<MoodScoreStat & { key: string; label: string }>
  compact?: boolean
}) {
  const { t } = useTranslation('mood')
  const position = (value: number) => ((value - 1) / 4) * 100
  return (
    <div
      className={['mood-rangechart', compact ? 'mood-rangechart--compact' : ''].join(
        ' ',
      )}
    >
      <div className="mood-dow__scale" aria-hidden="true">
        <span>5</span>
        <span>4</span>
        <span>3</span>
        <span>2</span>
        <span>1</span>
      </div>
      <div className="mood-dow" role="img" aria-label={label}>
        {points.map((point) => {
          const hasPoint = statHasData(point)
          return (
            <div key={point.key} className="mood-dow__col">
              <div className="mood-dow__track">
                {hasPoint && point.min != null && point.max != null ? (
                  <span
                    className="mood-dow__range"
                    style={{
                      bottom: `${position(point.min)}%`,
                      top: `${100 - position(point.max)}%`,
                      background: scoreColor(point.average ?? point.max),
                    }}
                  />
                ) : null}
                {hasPoint && point.average != null ? (
                  <span
                    className="mood-dow__avg"
                    style={{ bottom: `${position(point.average)}%` }}
                  />
                ) : null}
              </div>
              <div className="mood-dow__cap">
                <span className="mood-dow__avgval">{formatStat(point.average)}</span>
                <span className="mood-dow__lbl">{point.label}</span>
              </div>
            </div>
          )
        })}
      </div>
      <ul className="mood-sr-only">
        {points.map((point) => (
          <li key={point.key}>
            {point.label}:{' '}
            {statHasData(point)
              ? t('dashboard.charts.scoreSummary', {
                  min: formatStat(point.min),
                  average: formatStat(point.average),
                  max: formatStat(point.max),
                })
              : t('dashboard.charts.noData')}
          </li>
        ))}
      </ul>
    </div>
  )
}

function DistributionCard({
  criterion,
  buckets,
}: {
  criterion: CriteriaKey
  buckets: MoodDistributionBucket[]
}) {
  const { t } = useTranslation('mood')
  return (
    <div className="mood-card">
      <div className="mood-card__head">
        <span className="mood-card__title">{t(`criteria.${criterion}.label`)}</span>
        <span className="mood-card__sub">
          {t('dashboard.charts.distribution.subtitle')}
        </span>
      </div>
      <DistributionChart criterion={criterion} buckets={buckets} />
    </div>
  )
}

function DistributionChart({
  criterion,
  buckets,
}: {
  criterion: CriteriaKey
  buckets: MoodDistributionBucket[]
}) {
  const { t } = useTranslation('mood')
  const rows = criteriaValues[criterion].map((value) => ({
    value,
    count: buckets.find((bucket) => bucket.value === value)?.count ?? 0,
  }))
  const total = countEntries(rows)
  const max = Math.max(...rows.map((row) => row.count), 1)
  return (
    <div
      className="mood-dist"
      role="img"
      aria-label={t(`dashboard.charts.distribution.${criterion}`)}
    >
      {rows.map((row) => {
        const tone = toneFor(criterion, row.value)
        const [, fg] = moodToneVars[tone]
        const pct = total === 0 ? 0 : Math.round((row.count / total) * 100)
        return (
          <div key={row.value} className="mood-dist__row">
            <span className="mood-dist__lbl">
              {t(`criteria.${criterion}.${row.value}`)}
            </span>
            <div className="mood-dist__track">
              <div
                className="mood-dist__fill"
                style={{
                  width: `${(row.count / max) * 100}%`,
                  background: fg,
                }}
              />
            </div>
            <span className="mood-dist__pct">{pct}%</span>
          </div>
        )
      })}
    </div>
  )
}

function EvolutionCard({
  criterion,
  points,
}: {
  criterion: CriteriaKey
  points: MoodCriteriaEvolutionPoint[]
}) {
  const { t, i18n } = useTranslation('mood')
  const values = criteriaValues[criterion]
  const max = Math.max(
    ...points.map((point) =>
      values.reduce(
        (total, value) => total + getEvolutionCount(point, criterion, value),
        0,
      ),
    ),
    1,
  )

  return (
    <div className="mood-card">
      <div className="mood-card__head">
        <span className="mood-card__title">
          {t('dashboard.charts.evolution.title', {
            criterion: t(`criteria.${criterion}.label`),
          })}
        </span>
        <span className="mood-card__sub">
          {t('dashboard.charts.evolution.subtitle')}
        </span>
      </div>
      <div
        className="mood-evo"
        role="img"
        aria-label={t('dashboard.charts.evolution.aria', {
          criterion: t(`criteria.${criterion}.label`),
        })}
      >
        {points.map((point) => {
          const total = values.reduce(
            (sum, value) => sum + getEvolutionCount(point, criterion, value),
            0,
          )
          return (
            <div key={point.interval} className="mood-evo__col">
              <div className="mood-evo__track">
                {values.map((value) => {
                  const count = getEvolutionCount(point, criterion, value)
                  const [, fg] = moodToneVars[toneFor(criterion, value)]
                  return count > 0 ? (
                    <span
                      key={value}
                      className="mood-evo__seg"
                      style={{
                        height: `${(count / max) * 100}%`,
                        background: fg,
                      }}
                    />
                  ) : null
                })}
              </div>
              <span className="mood-evo__total">{total || '·'}</span>
              <span className="mood-evo__lbl">
                {intervalLabel(point.interval, i18n.language)}
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}

function toneFor(criterion: CriteriaKey, value: string): MoodTone {
  switch (criterion) {
    case 'energy':
      return criteriaTones.energy[value as MoodEnergy]
    case 'alignment':
      return criteriaTones.alignment[value as MoodAlignment]
    case 'direction':
      return criteriaTones.direction[value as MoodDirection]
    case 'source':
      return criteriaTones.source[value as MoodSource]
  }
}

function getEvolutionCount(
  point: MoodCriteriaEvolutionPoint,
  criterion: CriteriaKey,
  value: string,
): number {
  switch (criterion) {
    case 'energy':
      return point.energy[value as MoodEnergy] ?? 0
    case 'alignment':
      return point.alignment[value as MoodAlignment] ?? 0
    case 'direction':
      return point.direction[value as MoodDirection] ?? 0
    case 'source':
      return point.source[value as MoodSource] ?? 0
  }
}
