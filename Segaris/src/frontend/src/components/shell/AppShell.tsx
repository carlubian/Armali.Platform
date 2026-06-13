import { ArrowLeft, LogOut, UserRound } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import armaliLogo from '@/assets/armali-logo.png'
import { Avatar, IconButton, Tooltip } from '@/components/ui'

import './AppShell.css'

export function AppShell() {
  const { t } = useTranslation('platform')
  const { session, signOut } = useSession()
  const location = useLocation()
  const navigate = useNavigate()
  const isLauncher = location.pathname === '/'

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
              label={t('auth.signOut')}
              icon={<LogOut />}
              onClick={() => void signOut()}
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
      <Outlet />
    </div>
  )
}
