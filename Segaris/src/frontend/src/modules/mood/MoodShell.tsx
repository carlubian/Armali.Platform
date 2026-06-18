import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router-dom'

import { moodDashboardRoutePath, moodLogRoutePath } from '@/app/api/mood'

import './MoodPage.css'

/** Log ⇄ Dashboard switcher, rendered as router links so each view is a real URL. */
function MoodViewNav() {
  const { t } = useTranslation('mood')
  const className = ({ isActive }: { isActive: boolean }) =>
    ['mood-seg__btn', isActive ? 'is-active' : ''].filter(Boolean).join(' ')
  return (
    <nav className="mood-seg" aria-label={t('nav.label')}>
      <NavLink to={moodLogRoutePath} className={className} end>
        {t('nav.log')}
      </NavLink>
      <NavLink to={moodDashboardRoutePath} className={className} end>
        {t('nav.dashboard')}
      </NavLink>
    </nav>
  )
}

interface MoodShellProps {
  eyebrow: string
  title: string
  description: string
  /** View-specific controls (week navigation, new-entry action, period nav). */
  controls?: ReactNode
  children: ReactNode
}

/** Shared immersive Mood frame: header, the Log/Dashboard nav, and a controls row. */
export function MoodShell({
  eyebrow,
  title,
  description,
  controls,
  children,
}: MoodShellProps) {
  return (
    <main className="seg-mood armali-aurora">
      <section className="seg-mood__head">
        <div>
          <div className="armali-eyebrow">{eyebrow}</div>
          <h1>{title}</h1>
          <p>{description}</p>
        </div>
        <MoodViewNav />
      </section>
      {controls != null && <section className="seg-mood__controls">{controls}</section>}
      {children}
    </main>
  )
}
