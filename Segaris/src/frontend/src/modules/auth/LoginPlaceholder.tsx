import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import armaliLogo from '@/assets/armali-logo.png'
import { Card } from '@/components/ui'
import { LoadingScreen } from '@/components/feedback/LoadingScreen'

export function LoginPlaceholder() {
  const { t } = useTranslation('platform')
  const { status } = useSession()
  if (status === 'loading') return <LoadingScreen />
  if (status === 'authenticated') return <Navigate to="/" replace />

  return (
    <main className="seg-system-screen armali-aurora">
      <Card glass className="seg-state-card">
        <img src={armaliLogo} alt="" width="72" height="72" />
        <h1>{t('auth.loginTitle')}</h1>
        <p>{t('auth.loginPending')}</p>
      </Card>
    </main>
  )
}
