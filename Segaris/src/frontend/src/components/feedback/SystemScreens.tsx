import { ArrowLeft, CloudOff, Compass, RefreshCw, ShieldAlert } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router-dom'

import { Button } from '@/components/ui'

import './SystemScreens.css'

export function ServiceUnavailable({ onRetry }: { onRetry: () => void }) {
  const { t } = useTranslation('platform')
  return (
    <main className="seg-system-screen armali-aurora">
      <section className="seg-state-card">
        <span className="seg-state-icon seg-state-icon--danger">
          <CloudOff size={36} />
        </span>
        <div className="armali-eyebrow">{t('errors.unavailableEyebrow')}</div>
        <h1>{t('errors.unavailableTitle')}</h1>
        <p>{t('errors.unavailableBody')}</p>
        <Button iconLeft={<RefreshCw size={17} />} onClick={onRetry}>
          {t('common.tryAgain')}
        </Button>
      </section>
    </main>
  )
}

export function AccessDenied() {
  const { t } = useTranslation('platform')
  const navigate = useNavigate()
  return (
    <main className="seg-system-screen armali-aurora">
      <section className="seg-state-card">
        <span className="seg-state-icon seg-state-icon--danger">
          <ShieldAlert size={36} />
        </span>
        <div className="armali-eyebrow">{t('errors.accessDeniedEyebrow')}</div>
        <h1>{t('errors.accessDeniedTitle')}</h1>
        <p>{t('errors.accessDeniedBody')}</p>
        <Button iconLeft={<ArrowLeft size={17} />} onClick={() => void navigate('/')}>
          {t('common.returnToLauncher')}
        </Button>
      </section>
    </main>
  )
}

export function NotFound() {
  const { t } = useTranslation('platform')
  const navigate = useNavigate()
  const location = useLocation()
  return (
    <main className="seg-system-screen armali-aurora">
      <section className="seg-state-card">
        <span className="seg-state-icon seg-state-icon--gold">
          <Compass size={36} />
        </span>
        <div className="seg-state-code">404</div>
        <h1>{t('errors.notFoundTitle')}</h1>
        <p>{t('errors.notFoundBody')}</p>
        <Button iconLeft={<ArrowLeft size={17} />} onClick={() => void navigate('/')}>
          {t('common.returnToLauncher')}
        </Button>
        <div className="seg-state-meta">{location.pathname}</div>
      </section>
    </main>
  )
}
