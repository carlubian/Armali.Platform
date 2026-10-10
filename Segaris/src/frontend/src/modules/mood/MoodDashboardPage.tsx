import { ChevronLeft, ChevronRight, RotateCcw } from 'lucide-react'
import type { CSSProperties } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'

import type {
  MoodAlignment,
  MoodCriteriaEvolutionPoint,
  MoodDashboardScale,
  MoodDirection,
  MoodDistributionBucket,
  MoodEnergy,
  MoodScoreStat,
  MoodScoreSummary,
  MoodSource,
} from '@/app/api/mood'
import {
  moodAlignments,
  moodDashboardScales,
  moodDirections,
  moodEnergies,
  moodScoreMax,
  moodScoreMin,
  moodScores,
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

function formatSpread(value: number | null): string {
  return value == null ? '' : ` ±${value.toFixed(1)}`
}

/** Clamps a derived score (such as average ± σ) to the 0–5 score axis. */
function clampScore(value: number): number {
  return Math.max(moodScoreMin, Math.min(moodScoreMax, value))
}

/** Qualitative reading of the period's standard deviation on the 0–5 scale. */
function spreadLevel(standardDeviation: number): 'steady' | 'some' | 'wide' {
  if (standardDeviation < 0.6) return 'steady'
  if (standardDeviation < 1) return 'some'
  return 'wide'
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

  const isEmpty = dashboard != null && dashboard.entryCount === 0

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
          <div className="mood-sdtop">
            <ScoreSpreadLead
              score={dashboard.score}
              entryCount={dashboard.entryCount}
            />
            <ScoreSpreadCard
              title={t('dashboard.charts.dayOfWeek.title')}
              label={t('dashboard.charts.dayOfWeek.aria')}
              points={dayOrder.map((day) => ({
                key: String(day),
                label: t(`dashboard.days.${day}`),
                ...emptyStat(),
                ...dashboard.scoreByDayOfWeek.find((row) => row.dayOfWeek === day),
              }))}
            />
          </div>
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
          <ScoreSpreadCard
            title={t('dashboard.charts.interval.title')}
            subtitle={t(`dashboard.charts.interval.subtitle.${state.scale}`)}
            label={t('dashboard.charts.interval.aria')}
            points={dashboard.scoreByInterval.map((row) => ({
              key: row.interval,
              label: intervalLabel(row.interval, i18n.language),
              ...row,
            }))}
          />
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
  return { min: null, average: null, max: null, standardDeviation: null }
}

type ScorePoint = MoodScoreStat & { key: string; label: string }

/** Lead card: period average and standard deviation over a 0–5 score histogram. */
function ScoreSpreadLead({
  score,
  entryCount,
}: {
  score: MoodScoreSummary
  entryCount: number
}) {
  const { t } = useTranslation('mood')
  const mean = score.average
  const sd = score.standardDeviation
  return (
    <div className="mood-card mood-sdlead">
      <div className="mood-sdlead__nums">
        <div className="mood-sdlead__metric">
          <div className="armali-eyebrow mood-sdlead__eyebrow--avg">
            {t('dashboard.summary.average')}
          </div>
          <div className="mood-chartcard__big mood-sdlead__big">{formatStat(mean)}</div>
        </div>
        <div className="mood-sdlead__metric">
          <div className="armali-eyebrow mood-sdlead__eyebrow--sd">
            {t('dashboard.summary.standardDeviation')}
          </div>
          <div className="mood-chartcard__big mood-sdlead__big">
            {sd == null ? (
              '·'
            ) : (
              <>
                <span className="mood-sdlead__pm" aria-hidden="true">
                  ±
                </span>
                {sd.toFixed(1)}
              </>
            )}
          </div>
        </div>
      </div>
      {mean != null && sd != null ? (
        <div className="mood-sdlead__note">
          <strong>{t(`dashboard.summary.spread.${spreadLevel(sd)}`)}</strong>
          {' — '}
          {t('dashboard.summary.spreadRange', {
            low: clampScore(mean - sd).toFixed(1),
            high: clampScore(mean + sd).toFixed(1),
          })}
        </div>
      ) : null}
      <ScoreHistogram histogram={score.histogram} mean={mean} sd={sd} />
      <small className="mood-sdlead__foot">
        {t('dashboard.summary.meta', {
          count: entryCount,
          low: score.min ?? '·',
          high: score.max ?? '·',
        })}
      </small>
    </div>
  )
}

/** Score 0–5 histogram with the average ± 1σ band drawn beneath on the same axis. */
function ScoreHistogram({
  histogram,
  mean,
  sd,
}: {
  histogram: number[]
  mean: number | null
  sd: number | null
}) {
  const { t } = useTranslation('mood')
  const max = Math.max(...histogram, 1)
  // Six equal bins: the centre of score `n` sits at (n + 0.5) / 6 of the axis.
  const x = (value: number) => ((value + 0.5) / moodScores.length) * 100
  return (
    <div className="mood-sdhist">
      <div
        className="mood-sdhist__bars"
        role="img"
        aria-label={t('dashboard.charts.histogram.aria')}
      >
        {histogram.map((count, index) => (
          <div key={index} className="mood-sdhist__col">
            <span
              className="mood-sdhist__bar"
              style={{
                height: `${Math.max((count / max) * 100, count > 0 ? 4 : 0)}%`,
                background: scoreColor(index),
              }}
            />
          </div>
        ))}
      </div>
      <div className="mood-sdhist__axis" aria-hidden="true">
        {mean != null && sd != null ? (
          <>
            <span
              className="mood-sdhist__band"
              style={{
                left: `${x(clampScore(mean - sd))}%`,
                right: `${100 - x(clampScore(mean + sd))}%`,
              }}
            />
            <span className="mood-sdhist__mean" style={{ left: `${x(mean)}%` }} />
          </>
        ) : null}
      </div>
      <div className="mood-sdhist__lbls" aria-hidden="true">
        {moodScores.map((value) => (
          <span key={value}>{value}</span>
        ))}
      </div>
      <ul className="mood-sr-only">
        {histogram.map((count, index) => (
          <li key={index}>
            {t('dashboard.charts.histogram.bin', { score: index, count })}
          </li>
        ))}
      </ul>
    </div>
  )
}

function ScoreSpreadCard({
  title,
  subtitle,
  label,
  points,
}: {
  title: string
  subtitle?: string
  label: string
  points: ScorePoint[]
}) {
  const { t } = useTranslation('mood')
  return (
    <div className="mood-card">
      <div className="mood-card__head">
        <span className="mood-card__titles">
          <span className="mood-card__title">{title}</span>
          {subtitle != null ? <span className="mood-card__sub">{subtitle}</span> : null}
        </span>
        <span className="mood-sdlegend" aria-hidden="true">
          <span>
            <i className="mood-sdlegend__band" />
            {t('dashboard.charts.legend.band')}
          </span>
          <span>
            <i className="mood-sdlegend__avg" />
            {t('dashboard.charts.legend.average')}
          </span>
          <span>
            <i className="mood-sdlegend__whisker" />
            {t('dashboard.charts.legend.range')}
          </span>
        </span>
      </div>
      <ScoreSpreadChart label={label} points={points} />
    </div>
  )
}

/** Per-slot score spread: thin min–max whisker, average ± 1σ band, average marker. */
function ScoreSpreadChart({ label, points }: { label: string; points: ScorePoint[] }) {
  const { t } = useTranslation('mood')
  const position = (value: number) => (value / moodScoreMax) * 100
  return (
    <div className="mood-rangechart">
      {/* The axis mirrors a column (plot + invisible caption) so the ticks stay
          aligned with the grid lines however tall the captions wrap. */}
      <div className="mood-dow__col mood-dowsd__axis" aria-hidden="true">
        <div className="mood-dowsd__plot">
          {moodScores.map((value) => (
            <span
              key={value}
              className="mood-dowsd__tick"
              style={{ bottom: `${position(value)}%` }}
            >
              {value}
            </span>
          ))}
        </div>
        <div className="mood-dow__cap mood-dowsd__phantom">
          <span className="mood-dow__avgval">
            0.0<small className="mood-dowsd__sd"> ±0.0</small>
          </span>
          <span className="mood-dow__lbl">M</span>
        </div>
      </div>
      <div
        className="mood-dow mood-dowsd"
        role="img"
        aria-label={label}
        style={{ gridTemplateColumns: `repeat(${points.length}, minmax(0, 1fr))` }}
      >
        {points.map((point) => (
          <div key={point.key} className="mood-dow__col">
            <div className="mood-dowsd__plot">
              {moodScores.map((value) => (
                <span
                  key={value}
                  className="mood-dowsd__grid"
                  style={{ bottom: `${position(value)}%` }}
                />
              ))}
              {point.min != null && point.max != null ? (
                <>
                  <span
                    className="mood-dowsd__whisker"
                    style={{
                      bottom: `${position(point.min)}%`,
                      top: `${100 - position(point.max)}%`,
                    }}
                  />
                  <span
                    className="mood-dowsd__cap"
                    style={{ bottom: `${position(point.max)}%` }}
                  />
                  <span
                    className="mood-dowsd__cap"
                    style={{ bottom: `${position(point.min)}%` }}
                  />
                </>
              ) : null}
              {point.average != null && point.standardDeviation != null ? (
                <span
                  className="mood-dowsd__band"
                  style={
                    {
                      bottom: `${position(clampScore(point.average - point.standardDeviation))}%`,
                      top: `${100 - position(clampScore(point.average + point.standardDeviation))}%`,
                      '--band': scoreColor(point.average),
                    } as CSSProperties
                  }
                />
              ) : null}
              {point.average != null ? (
                <span
                  className="mood-dowsd__avg"
                  style={{ bottom: `${position(point.average)}%` }}
                />
              ) : null}
            </div>
            <div className="mood-dow__cap">
              <span className="mood-dow__avgval">
                {formatStat(point.average)}
                <small className="mood-dowsd__sd">
                  {formatSpread(point.standardDeviation)}
                </small>
              </span>
              <span className="mood-dow__lbl">{point.label}</span>
            </div>
          </div>
        ))}
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
                  standardDeviation: formatStat(point.standardDeviation),
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
