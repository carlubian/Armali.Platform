import { UserRound, Users } from 'lucide-react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import { Badge } from '@/components/ui'

import { ModuleCard, type ModuleCardModel } from './ModuleCard'

import './LauncherPage.css'

export function LauncherPage() {
  const { t } = useTranslation('platform')
  const { session } = useSession()
  const navigate = useNavigate()
  const roles = session?.roles ?? []
  const modules = useMemo<ModuleCardModel[]>(
    () => [
      {
        key: 'profile',
        title: t('launcher.modules.profile.title'),
        description: t('launcher.modules.profile.description'),
        actionLabel: t('launcher.open'),
        href: '/profile',
        icon: UserRound,
        tone: 'aqua',
      },
      {
        key: 'users',
        title: t('launcher.modules.users.title'),
        description: t('launcher.modules.users.description'),
        actionLabel: t('launcher.open'),
        href: '/users',
        icon: Users,
        tone: 'gold',
        requiresRole: 'Admin',
      },
    ],
    [t],
  )
  const visibleModules = modules.filter(
    (module) => module.requiresRole == null || roles.includes(module.requiresRole),
  )

  return (
    <main className="seg-launcher armali-aurora">
      <section className="seg-launcher__head">
        <div>
          <div className="armali-eyebrow">
            {t('launcher.greeting', { name: session?.displayName })}
          </div>
          <h1>{t('launcher.title')}</h1>
          <p>{t('launcher.description')}</p>
        </div>
        <Badge tone="neutral">
          {t('launcher.moduleCount', { count: visibleModules.length })}
        </Badge>
      </section>
      <section className="seg-modules" aria-label={t('launcher.availableModules')}>
        {visibleModules.map((module) => (
          <ModuleCard
            key={module.key}
            module={module}
            onOpen={(href) => void navigate(href)}
          />
        ))}
      </section>
    </main>
  )
}
