import { useTranslation } from 'react-i18next'

import type { MoodEntry } from '@/app/api/mood'

import {
  alignmentTone,
  directionTone,
  energyTone,
  moodToneVars,
  scoreColor,
  scoreTone,
  sourceTone,
  type MoodTone,
} from './criteria'

/** Translated label for a derived-emotion code, falling back to the raw code. */
export function useEmotionLabel() {
  const { t } = useTranslation('mood')
  return (code: string) => t(`emotions.${code}`, { defaultValue: code })
}

interface ScoreChipProps {
  score: number
  size?: number
  className?: string
}

/** A rounded chip showing a 1–5 score (or a one-decimal average) in its tone. */
export function ScoreChip({ score, size = 34, className }: ScoreChipProps) {
  const [bg, fg] = moodToneVars[scoreTone(score)]
  const text = Number.isInteger(score) ? String(score) : score.toFixed(1)
  return (
    <span
      className={['mood-scorechip', className].filter(Boolean).join(' ')}
      style={{ width: size, height: size, background: bg, color: fg }}
    >
      {text}
    </span>
  )
}

function Pill({ label, tone }: { label: string; tone: MoodTone }) {
  const [bg, fg] = moodToneVars[tone]
  return (
    <span className="mood-pill" style={{ background: bg, color: fg }}>
      {label}
    </span>
  )
}

/** The four fixed criteria of an entry rendered as labelled, toned pills. */
export function CriteriaPills({ entry }: { entry: MoodEntry }) {
  const { t } = useTranslation('mood')
  return (
    <div className="mood-pills">
      <Pill
        label={t(`criteria.energy.${entry.energy}`)}
        tone={energyTone[entry.energy]}
      />
      <Pill
        label={t(`criteria.alignment.${entry.alignment}`)}
        tone={alignmentTone[entry.alignment]}
      />
      <Pill
        label={t(`criteria.direction.${entry.direction}`)}
        tone={directionTone[entry.direction]}
      />
      <Pill
        label={t(`criteria.source.${entry.source}`)}
        tone={sourceTone[entry.source]}
      />
    </div>
  )
}

export interface WeekChartDay {
  /** Short weekday label (Mon … Sun). */
  label: string
  /** Average score for the day, or `null` when the user logged nothing. */
  average: number | null
}

/**
 * Compact weekly average-score chart: seven bars, one per day. Days without
 * entries render as a dashed gap rather than a zero. The bars are decorative; a
 * visually hidden list gives screen-reader users the same per-day values.
 */
export function WeekScoreChart({ days }: { days: WeekChartDay[] }) {
  const { t } = useTranslation('mood')
  return (
    <div className="mood-weekchart">
      <div
        className="mood-weekchart__plot"
        role="img"
        aria-label={t('log.chart.caption')}
      >
        {days.map((day, index) => {
          const filled = day.average != null
          const height = filled ? ((day.average! - 0.5) / 4.5) * 100 : 0
          return (
            <div key={index} className="mood-weekchart__col">
              <div className="mood-weekchart__track">
                {filled ? (
                  <span
                    className="mood-weekchart__bar"
                    style={{
                      height: `${height}%`,
                      background: scoreColor(day.average!),
                    }}
                  />
                ) : (
                  <span className="mood-weekchart__gap" />
                )}
              </div>
              <span className="mood-weekchart__val">
                {filled ? day.average!.toFixed(1) : '·'}
              </span>
              <span className="mood-weekchart__lbl">{day.label}</span>
            </div>
          )
        })}
      </div>
      <ul className="mood-sr-only">
        {days.map((day, index) => (
          <li key={index}>
            {day.label}:{' '}
            {day.average == null ? t('log.chart.noDay') : day.average.toFixed(1)}
          </li>
        ))}
      </ul>
    </div>
  )
}
