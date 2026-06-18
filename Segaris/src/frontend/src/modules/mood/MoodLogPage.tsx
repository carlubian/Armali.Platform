import { useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'

import { isApiError } from '@/app/api/errors'
import type { MoodEntry } from '@/app/api/mood'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Button, Spinner, Toast } from '@/components/ui'

import { MoodEntryDialog } from './MoodEntryDialog'
import {
  CriteriaPills,
  ScoreChip,
  WeekScoreChart,
  useEmotionLabel,
} from './MoodPrimitives'
import { MoodShell } from './MoodShell'
import { householdToday } from './entryForm'
import {
  addWeeks,
  mondayOf,
  parseEntryDialogState,
  parseSelectedWeek,
  weekDates,
  weekRangeQuery,
} from './logState'
import { moodKeys, useMoodWeek } from './queries'

type ToastKind = 'created' | 'updated' | 'deleted'

interface ToastState {
  kind: ToastKind
  date: string
}

const shortDow = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

interface DayModel {
  iso: string
  dow: string
  dayNumber: number
  isToday: boolean
  entries: MoodEntry[]
  average: number | null
}

export function MoodLogPage() {
  const { t, i18n } = useTranslation('mood')
  const [params, setParams] = useSearchParams()
  const queryClient = useQueryClient()
  const [toast, setToast] = useState<ToastState | null>(null)

  const today = householdToday()
  const monday = parseSelectedWeek(params, today)
  const dialog = parseEntryDialogState(params)
  const range = weekRangeQuery(monday)

  const weekQuery = useMoodWeek(range)

  const dayFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat(i18n.language, {
        day: 'numeric',
        month: 'short',
        timeZone: 'UTC',
      }),
    [i18n.language],
  )
  const formatDay = (iso: string) => {
    const [year, month, day] = iso.split('-').map(Number)
    return dayFormatter.format(new Date(Date.UTC(year, month - 1, day)))
  }

  const days = useMemo<DayModel[]>(() => {
    const entries = weekQuery.data?.entries ?? []
    const averages = new Map(
      (weekQuery.data?.dailyAverages ?? []).map((bucket) => [
        bucket.entryDate,
        bucket.averageScore,
      ]),
    )
    return weekDates(monday).map((iso, index) => ({
      iso,
      dow: shortDow[index],
      dayNumber: Number(iso.slice(8, 10)),
      isToday: iso === today,
      entries: entries.filter((entry) => entry.entryDate === iso),
      average: averages.get(iso) ?? null,
    }))
  }, [weekQuery.data, monday, today])

  const totalEntries = weekQuery.data?.entries.length ?? 0
  const weekAverage = useMemo(() => {
    const scores = (weekQuery.data?.entries ?? []).map((entry) => entry.score)
    if (scores.length === 0) return null
    return scores.reduce((total, score) => total + score, 0) / scores.length
  }, [weekQuery.data])

  const updateParams = (mutate: (next: URLSearchParams) => void) => {
    const next = new URLSearchParams(params)
    mutate(next)
    setParams(next, { replace: false })
  }

  const goToWeek = (nextMonday: string) =>
    updateParams((next) => {
      next.set('week', nextMonday)
      next.delete('newEntry')
      next.delete('entryId')
    })

  const openCreate = () =>
    updateParams((next) => {
      next.delete('entryId')
      next.set('newEntry', 'true')
    })

  const openEdit = (entryId: number) =>
    updateParams((next) => {
      next.delete('newEntry')
      next.set('entryId', String(entryId))
    })

  const closeDialog = () =>
    updateParams((next) => {
      next.delete('newEntry')
      next.delete('entryId')
    })

  // A mutation can touch the visible week and any dashboard aggregate, so both
  // query families are refreshed. Mood never contributes launcher attention.
  const refreshAfterMutation = () => {
    void queryClient.invalidateQueries({ queryKey: moodKeys.entries() })
    void queryClient.invalidateQueries({ queryKey: moodKeys.dashboard() })
  }

  const handleMutated = (kind: ToastKind, entry: MoodEntry) => {
    if (kind !== 'deleted') queryClient.setQueryData(moodKeys.entry(entry.id), entry)
    refreshAfterMutation()
    setToast({ kind, date: entry.entryDate })
    closeDialog()
  }

  if (weekQuery.isError) {
    const error = weekQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void weekQuery.refetch()} />
    }
  }

  const controls = (
    <div className="mood-controls">
      <div className="mood-nav" role="group" aria-label={t('log.week.label')}>
        <button
          type="button"
          className="mood-nav__btn"
          aria-label={t('log.week.previous')}
          onClick={() => goToWeek(addWeeks(monday, -1))}
        >
          <ChevronLeft size={18} aria-hidden="true" />
        </button>
        <span className="mood-nav__label">
          {t('log.week.range', {
            start: formatDay(range.from),
            end: formatDay(range.to),
          })}
        </span>
        <button
          type="button"
          className="mood-nav__btn"
          aria-label={t('log.week.next')}
          onClick={() => goToWeek(addWeeks(monday, 1))}
        >
          <ChevronRight size={18} aria-hidden="true" />
        </button>
      </div>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => goToWeek(mondayOf(today))}
        disabled={monday === mondayOf(today)}
      >
        {t('log.week.today')}
      </Button>
      <Button iconLeft={<Plus size={16} />} onClick={openCreate}>
        {t('log.newEntry')}
      </Button>
    </div>
  )

  return (
    <MoodShell
      eyebrow={t('log.eyebrow')}
      title={t('log.title')}
      description={t('log.description')}
      controls={controls}
    >
      {weekQuery.isPending ? (
        <div className="seg-mood__loading">
          <Spinner />
          <span>{t('log.states.loading')}</span>
        </div>
      ) : weekQuery.isError ? (
        <p className="seg-mood__error" role="alert">
          {t('log.states.loadError')}
        </p>
      ) : (
        <>
          <div className="mood-chartcard">
            <div className="mood-chartcard__lead">
              <div className="armali-eyebrow">{t('log.chart.eyebrow')}</div>
              <div className="mood-chartcard__big">
                {weekAverage == null ? t('log.chart.empty') : weekAverage.toFixed(1)}
              </div>
              <small>{t('log.chart.summary', { count: totalEntries })}</small>
            </div>
            <WeekScoreChart
              days={days.map((day) => ({ label: day.dow, average: day.average }))}
            />
          </div>

          <div className="mood-board">
            {days.map((day) => (
              <DayColumn key={day.iso} day={day} onOpen={openEdit} />
            ))}
          </div>
        </>
      )}

      {dialog.mode !== 'closed' && (
        <MoodEntryDialog
          mode={dialog.mode}
          entryId={dialog.mode === 'edit' ? dialog.entryId : undefined}
          today={today}
          onClose={closeDialog}
          onCreated={(entry) => handleMutated('created', entry)}
          onSaved={(entry) => handleMutated('updated', entry)}
          onDeleted={(entry) => handleMutated('deleted', entry)}
        />
      )}

      {toast != null && (
        <div className="seg-mood__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            onClose={() => setToast(null)}
            closeLabel={t('editor.actions.cancel')}
          >
            {t(`toast.${toast.kind}Body`, { date: formatDay(toast.date) })}
          </Toast>
        </div>
      )}
    </MoodShell>
  )
}

interface DayColumnProps {
  day: DayModel
  onOpen: (entryId: number) => void
}

function DayColumn({ day, onOpen }: DayColumnProps) {
  const { t } = useTranslation('mood')
  const emotionLabel = useEmotionLabel()
  return (
    <div
      className={['mood-daycol', day.isToday ? 'is-today' : '']
        .filter(Boolean)
        .join(' ')}
    >
      <div className="mood-daycol__head">
        <span className="mood-daycol__title">
          <span className="mood-daycol__dow">{day.dow}</span>
          <span className="mood-daycol__date">{day.dayNumber}</span>
        </span>
        {day.average != null && <ScoreChip score={day.average} size={26} />}
      </div>
      <div className="mood-daycol__body">
        {day.entries.length === 0 ? (
          <p className="mood-daycol__empty">
            {day.isToday ? t('log.day.todayEmpty') : t('log.day.empty')}
          </p>
        ) : (
          day.entries.map((entry) => {
            const emotion = emotionLabel(entry.derivedEmotion)
            return (
              <button
                key={entry.id}
                type="button"
                className="mood-entry"
                onClick={() => onOpen(entry.id)}
                aria-label={t('log.entry.open', { score: entry.score, emotion })}
              >
                <div className="mood-entry__top">
                  <ScoreChip score={entry.score} size={28} />
                  <span className="mood-entry__emotion">{emotion}</span>
                </div>
                <CriteriaPills entry={entry} />
              </button>
            )
          })
        )}
      </div>
    </div>
  )
}
