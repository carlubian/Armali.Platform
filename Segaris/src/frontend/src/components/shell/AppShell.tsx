import { ArrowLeft, LoaderCircle, LogOut, UserRound } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import armaliLogo from '@/assets/armali-logo.png'
import { Avatar, IconButton, Toast, Tooltip } from '@/components/ui'

import './AppShell.css'

export function AppShell() {
  const { t } = useTranslation('platform')
  const { session, signOut } = useSession()
  const location = useLocation()
  const navigate = useNavigate()
  const isLauncher = location.pathname === '/'
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [signOutFailed, setSignOutFailed] = useState(false)

  async function handleSignOut() {
    setIsSigningOut(true)
    setSignOutFailed(false)
    try {
      await signOut()
    } catch {
      setSignOutFailed(true)
      setIsSigningOut(false)
    }
  }

  return (
    <div className="seg-shell">
      <header className="seg-topbar">
        <div className="seg-topbar__left">
          {isLauncher ? (
            <Link className="seg-brand" to="/" aria-label={t('app.name')}>
              <img src={armaliLogo} alt="" />
              <span>{t('app.name')}</span>
            </Link>
          ) : (
            <button
              className="seg-back"
              type="button"
              onClick={() => void navigate('/')}
            >
              <ArrowLeft size={16} /> {t('shell.launcher')}
            </button>
          )}
        </div>
        <div className="seg-topbar__right">
          <Tooltip label={t('shell.profile')} side="bottom">
            <IconButton
              label={t('shell.profile')}
              icon={<UserRound />}
              onClick={() => void navigate('/profile')}
            />
          </Tooltip>
          <Tooltip label={t('auth.signOut')} side="bottom">
            <IconButton
              label={isSigningOut ? t('auth.signingOut') : t('auth.signOut')}
              icon={isSigningOut ? <LoaderCircle className="seg-spin" /> : <LogOut />}
              onClick={() => void handleSignOut()}
              disabled={isSigningOut}
              aria-busy={isSigningOut}
            />
          </Tooltip>
          <span className="seg-divider" />
          <Avatar
            name={session?.displayName ?? session?.userName ?? ''}
            src={session?.avatarUrl ?? undefined}
            status="online"
          />
        </div>
      </header>
      {signOutFailed && (
        <div className="seg-shell__notice">
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
      <Outlet />
    </div>
  )
}
