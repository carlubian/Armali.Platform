import {
  Feather,
  Images,
  ListChecks,
  Settings,
  Tags,
  Upload,
  type LucideIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useLocation } from 'react-router-dom'

import './AppShell.css'

interface NavEntry {
  /** Key under `shell.nav` in the platform namespace. */
  readonly id: string
  readonly icon: LucideIcon
  /** Router path, or `null` while the section has not been built yet. */
  readonly path: string | null
}

/**
 * Primary navigation, in the order the M1 prototype fixes it.
 *
 * Only the gallery exists at this stage. The remaining sections are rendered as
 * disabled entries so the shell matches the prototype's persistent sidebar
 * without inventing screens that belong to later phases.
 */
const navEntries: readonly NavEntry[] = [
  { id: 'gallery', icon: Images, path: '/' },
  { id: 'upload', icon: Upload, path: null },
  { id: 'review', icon: ListChecks, path: null },
  { id: 'tags', icon: Tags, path: null },
  { id: 'settings', icon: Settings, path: null },
]

/**
 * Blackwing application shell: a persistent 264px sidebar, a topbar carrying the
 * current section's eyebrow and title, and an aurora-backed content area that
 * hosts the routed screen.
 */
export function AppShell() {
  const { t } = useTranslation('platform')
  const location = useLocation()
  const activeEntry =
    navEntries.find((entry) => entry.path === location.pathname) ?? navEntries[0]

  return (
    <div className="bw-shell">
      <aside className="bw-sidebar">
        <div className="bw-brand">
          <span className="bw-brand__mark">
            <Feather size={18} aria-hidden="true" />
          </span>
          <span className="bw-brand__name">{t('app.name')}</span>
        </div>
        <nav className="bw-nav" aria-label={t('shell.primaryNavigation')}>
          {navEntries.map(({ id, icon: Icon, path }) =>
            path === null ? (
              <button
                key={id}
                type="button"
                className="bw-nav__item bw-nav__item--pending"
                disabled
                title={t('shell.comingLater')}
              >
                <Icon size={18} aria-hidden="true" />
                <span className="bw-nav__label">{t(`shell.nav.${id}.label`)}</span>
              </button>
            ) : (
              <NavLink key={id} to={path} className="bw-nav__item" end>
                <Icon size={18} aria-hidden="true" />
                <span className="bw-nav__label">{t(`shell.nav.${id}.label`)}</span>
              </NavLink>
            ),
          )}
        </nav>
      </aside>
      <div className="bw-main">
        <header className="bw-topbar">
          <div>
            <div className="armali-eyebrow bw-topbar__eyebrow">
              {t(`shell.nav.${activeEntry.id}.eyebrow`)}
            </div>
            <h1 className="bw-topbar__title">
              {t(`shell.nav.${activeEntry.id}.title`)}
            </h1>
          </div>
        </header>
        {/* The aurora layer must not scroll away with the content: its blobs are
            absolutely positioned past the container edges, so the clipping
            container stays fixed and an inner element does the scrolling. */}
        <main className="bw-body armali-aurora">
          <div className="bw-body__scroll">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
