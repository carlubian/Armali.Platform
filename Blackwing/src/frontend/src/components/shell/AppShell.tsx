import {
  Feather,
  Images,
  ListChecks,
  LoaderCircle,
  LogOut,
  Settings,
  Tags,
  Upload,
  UsersRound,
  type LucideIcon,
} from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useLocation } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import { IconButton, Toast } from '@/components/ui'

import './AppShell.css'

interface NavEntry {
  /** Key under `shell.nav` in the platform namespace. */
  readonly id: string
  readonly icon: LucideIcon
  /** Router path, or `null` while the section has not been built yet. */
  readonly path: string | null
  /** When set, the entry only renders for an account holding this role. */
  readonly requiresRole?: string
}

/**
 * Primary navigation, in the order the M1 prototype fixes it.
 *
 * Only the gallery and account administration exist at this stage. The
 * remaining sections are rendered as disabled entries so the shell matches the
 * prototype's persistent sidebar without inventing screens that belong to later
 * phases. Administration is not one of them: it is hidden outright for a
 * non-administrator rather than shown disabled, because its very presence would
 * advertise a surface that account cannot reach.
 */
const navEntries: readonly NavEntry[] = [
  { id: 'gallery', icon: Images, path: '/' },
  { id: 'upload', icon: Upload, path: null },
  { id: 'review', icon: ListChecks, path: null },
  { id: 'tags', icon: Tags, path: null },
  { id: 'settings', icon: Settings, path: null },
  { id: 'admin', icon: UsersRound, path: '/admin/users', requiresRole: 'Admin' },
]

/**
 * Blackwing application shell: a persistent 264px sidebar, a topbar carrying the
 * current section's eyebrow and title plus the signed-in account, and an
 * aurora-backed content area that hosts the routed screen.
 */
export function AppShell() {
  const { t } = useTranslation('platform')
  const { session, signOut } = useSession()
  const location = useLocation()
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [signOutFailed, setSignOutFailed] = useState(false)

  const roles = session?.roles ?? []
  const visibleEntries = navEntries.filter(
    (entry) => entry.requiresRole === undefined || roles.includes(entry.requiresRole),
  )
  const activeEntry =
    visibleEntries.find((entry) => entry.path === location.pathname) ??
    visibleEntries[0]

  async function handleSignOut() {
    setIsSigningOut(true)
    setSignOutFailed(false)
    try {
      await signOut()
    } catch {
      // The session survived, so the shell stays put and says so; a silent
      // failure here would leave the reader believing they had signed out.
      setSignOutFailed(true)
      setIsSigningOut(false)
    }
  }

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
          {visibleEntries.map(({ id, icon: Icon, path }) =>
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
          <div className="bw-account">
            <span className="bw-account__identity">
              <span className="bw-account__name">{session?.displayName}</span>
              <span className="bw-account__role">
                {roles.includes('Admin')
                  ? t('admin.users.roleAdmin')
                  : t('admin.users.roleUser')}
              </span>
            </span>
            <IconButton
              label={isSigningOut ? t('auth.signingOut') : t('auth.signOut')}
              icon={
                isSigningOut ? (
                  <LoaderCircle className="bw-account__spin" aria-hidden="true" />
                ) : (
                  <LogOut aria-hidden="true" />
                )
              }
              onClick={() => void handleSignOut()}
              disabled={isSigningOut}
              aria-busy={isSigningOut}
            />
          </div>
        </header>
        {signOutFailed && (
          <div className="bw-shell__notice">
            <Toast
              tone="danger"
              role="alert"
              title={t('auth.signOutErrorTitle')}
              onClose={() => setSignOutFailed(false)}
              closeLabel={t('common.close')}
            >
              {t('auth.signOutErrorBody')}
            </Toast>
          </div>
        )}
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
