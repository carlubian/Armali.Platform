import { Activity, Moon, Users, type LucideIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import { isApiError } from '@/app/api/errors'
import type { WellnessCategory, WellnessDayTask } from '@/app/api/wellness'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Checkbox, Spinner, Toast, type BadgeTone } from '@/components/ui'

import { useToggleWellnessDayTask, useWellnessToday } from './queries'

import './WellnessPage.css'

const categoryTone: Record<WellnessCategory, BadgeTone> = {
  HealthAndBody: 'success',
  MindAndSleep: 'azure',
  PeopleAndWork: 'gold',
}

const categoryIcon: Record<WellnessCategory, LucideIcon> = {
  HealthAndBody: Activity,
  MindAndSleep: Moon,
  PeopleAndWork: Users,
}

export function WellnessPage() {
  const { t } = useTranslation('wellness')
  const todayQuery = useWellnessToday()
  const toggle = useToggleWellnessDayTask()
  const [toggleFailed, setToggleFailed] = useState(false)

  // Each attempt clears any prior toast and only re-raises a privacy-safe one when
  // the mutation itself reports failure, keeping the toast tied to the latest click.
  const handleToggle = (dayTaskId: number) => {
    setToggleFailed(false)
    toggle.mutate(dayTaskId, { onError: () => setToggleFailed(true) })
  }

  if (todayQuery.isError) {
    const error = todayQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void todayQuery.refetch()} />
    }
  }

  const data = todayQuery.data
  const tasks = data?.tasks ?? []
  const completed = tasks.filter((task) => task.completed).length
  const total = tasks.length
  const score = data?.score ?? null

  return (
    <main className="seg-wellness armali-aurora">
      <section className="seg-wellness__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>

      {todayQuery.isPending ? (
        <div className="seg-wellness__loading">
          <Spinner label={t('states.loading')} />
        </div>
      ) : todayQuery.isError ? (
        <p className="seg-wellness__error" role="alert">
          {t('states.loadError')}
        </p>
      ) : total === 0 ? (
        <EmptyState title={t('states.empty.title')} body={t('states.empty.body')} />
      ) : (
        <section className="seg-wellness__body">
          <ScoreRing score={score ?? 0} completed={completed} total={total} />
          <div className="seg-wellness__list-wrap">
            <h2 className="seg-wellness__list-title">{t('tasks.heading')}</h2>
            <ul className="seg-wellness__list">
              {tasks.map((task) => (
                <TaskRow
                  key={task.id}
                  task={task}
                  disabled={toggle.isPending}
                  onToggle={() => handleToggle(task.id)}
                />
              ))}
            </ul>
          </div>
        </section>
      )}

      {toggleFailed && (
        <div className="seg-wellness__toast">
          <Toast
            tone="danger"
            title={t('toast.error')}
            closeLabel={t('toast.close')}
            onClose={() => setToggleFailed(false)}
          >
            {t('toast.errorBody')}
          </Toast>
        </div>
      )}
    </main>
  )
}

interface ScoreRingProps {
  score: number
  completed: number
  total: number
}

function ScoreRing({ score, completed, total }: ScoreRingProps) {
  const { t } = useTranslation('wellness')
  const radius = 52
  const circumference = 2 * Math.PI * radius
  const clamped = Math.min(100, Math.max(0, score))
  const offset = circumference * (1 - clamped / 100)

  return (
    <div className="seg-wellness-score">
      <div
        className="seg-wellness-ring"
        role="img"
        aria-label={t('score.reading', { score: clamped, completed, total })}
      >
        <svg viewBox="0 0 120 120" aria-hidden="true">
          <circle className="seg-wellness-ring__track" cx="60" cy="60" r={radius} />
          <circle
            className="seg-wellness-ring__fill"
            cx="60"
            cy="60"
            r={radius}
            strokeDasharray={circumference}
            strokeDashoffset={offset}
          />
        </svg>
        <div className="seg-wellness-ring__center" aria-hidden="true">
          <span className="seg-wellness-ring__value">{clamped}</span>
          <span className="seg-wellness-ring__unit">%</span>
        </div>
      </div>
      <div className="seg-wellness-score__meta">
        <div className="armali-eyebrow">{t('score.eyebrow')}</div>
        <div className="seg-wellness-score__label">{t('score.label')}</div>
        <p className="seg-wellness-score__caption" aria-live="polite">
          {t('score.caption', { completed, total })}
        </p>
      </div>
    </div>
  )
}

interface TaskRowProps {
  task: WellnessDayTask
  disabled: boolean
  onToggle: () => void
}

function TaskRow({ task, disabled, onToggle }: TaskRowProps) {
  const { t } = useTranslation('wellness')
  const Icon = categoryIcon[task.category]
  return (
    <li className={'seg-wellness-task' + (task.completed ? ' is-done' : '')}>
      <Checkbox
        checked={task.completed}
        onChange={onToggle}
        disabled={disabled}
        aria-label={t('tasks.toggle', { name: task.name })}
      />
      <span className="seg-wellness-task__name">{task.name}</span>
      <Badge
        tone={categoryTone[task.category]}
        className="seg-wellness-task__cat"
        aria-label={t('tasks.category', { category: t(`category.${task.category}`) })}
      >
        <Icon size={13} aria-hidden="true" />
        {t(`category.${task.category}`)}
      </Badge>
    </li>
  )
}

function EmptyState({ title, body }: { title: string; body: ReactNode }) {
  return (
    <section className="seg-wellness-empty">
      <h2>{title}</h2>
      <p>{body}</p>
    </section>
  )
}
