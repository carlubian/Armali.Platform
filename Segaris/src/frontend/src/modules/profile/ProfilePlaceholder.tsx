import { useTranslation } from 'react-i18next'
import { useSession } from '@/app/session/SessionContext'
import { Avatar, Card } from '@/components/ui'

export function ProfilePlaceholder() {
  const { t } = useTranslation('platform')
  const { session } = useSession()
  return (
    <main className="seg-launcher">
      <Card glass>
        <Avatar
          name={session?.displayName}
          src={session?.avatarUrl ?? undefined}
          size="lg"
        />
        <h1>{t('shell.profile')}</h1>
        <p>{session?.userName}</p>
      </Card>
    </main>
  )
}
