import { ArrowLeft, CloudOff, Compass, RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { Button } from '@/components/ui'

import './SystemScreens.css'

/** The backend is unreachable or answering with a transient failure. */
export function ServiceUnavailable({ onRetry }: { onRetry: () => void }) {
  const { t } = useTranslation('platform')
  return (
    <main className="bw-system-screen armali-aurora">
      <section className="bw-state-card">
        <span className="bw-state-icon bw-state-icon--danger">
          <CloudOff size={36} aria-hidden="true" />
        </span>
        <div className="armali-eyebrow">{t('session.unavailableEyebrow')}</div>
        <h1>{t('session.unavailableTitle')}</h1>
        <p>{t('session.unavailableBody')}</p>
        <Button iconLeft={<RefreshCw size={17} aria-hidden="true" />} onClick={onRetry}>
          {t('common.tryAgain')}
        </Button>
      </section>
    </main>
  )
}

/**
 * The requested route does not exist for this account.
 *
 * A signed-in `User` who types `/admin/users` lands here rather than on an
 * access-denied screen: telling them the area exists but is closed to them
 * would confirm the shape of the administrative surface for no benefit.
 */
export function NotFound() {
  const { t } = useTranslation('platform')
  const navigate = useNavigate()
  return (
    <main className="bw-system-screen armali-aurora">
      <section className="bw-state-card">
        <span className="bw-state-icon bw-state-icon--gold">
          <Compass size={36} aria-hidden="true" />
        </span>
        <div className="bw-state-code">{t('session.notFoundCode')}</div>
        <h1>{t('session.notFoundTitle')}</h1>
        <p>{t('session.notFoundBody')}</p>
        <Button
          iconLeft={<ArrowLeft size={17} aria-hidden="true" />}
          onClick={() => void navigate('/')}
        >
          {t('session.returnToGallery')}
        </Button>
      </section>
    </main>
  )
}
