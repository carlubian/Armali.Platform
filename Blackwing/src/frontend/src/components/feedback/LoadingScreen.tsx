import { useTranslation } from 'react-i18next'

import { Spinner } from '@/components/ui'

import './SystemScreens.css'

/**
 * Shown while the session query is still in flight.
 *
 * The guards render this instead of the shell so a signed-out visitor never
 * sees a flash of application chrome before being sent to the login screen.
 */
export function LoadingScreen() {
  const { t } = useTranslation('platform')
  return (
    <main className="bw-system-screen" aria-busy="true">
      <Spinner size={40} label={t('session.loading')} />
    </main>
  )
}
