import { Users } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import { Badge, Button, Card } from '@/components/ui'

import './LauncherPage.css'

export function LauncherPage() {
  const { t } = useTranslation('platform')
  const { session } = useSession()
  const navigate = useNavigate()
  const isAdmin = session?.roles.includes('Admin') ?? false

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
        {isAdmin && (
          <Button
            variant="outline"
            iconLeft={<Users size={17} />}
            onClick={() => void navigate('/users')}
          >
            {t('admin.users.title')}
          </Button>
        )}
      </section>
    </main>
  )
}
