import { useTranslation } from 'react-i18next'

import { useSession } from '@/app/session/SessionContext'
import { Badge, Card } from '@/components/ui'

import './LauncherPage.css'

export function LauncherPage() {
  const { t } = useTranslation('platform')
  const { session } = useSession()

  return (
    <main className="seg-launcher armali-aurora">
      <section className="seg-launcher__head">
        <div>
          <div className="armali-eyebrow">{t('launcher.eyebrow')}</div>
          <h1>{t('launcher.title')}</h1>
          <p>{t('launcher.description')}</p>
        </div>
        <Badge tone="success" dot>
          {t('launcher.foundation')}
        </Badge>
      </section>
      <section className="seg-launcher__content" aria-label={t('launcher.title')}>
        <Card glass>
          <p>{session?.displayName}</p>
          <p>{t('launcher.foundation')}</p>
        </Card>
      </section>
    </main>
  )
}
