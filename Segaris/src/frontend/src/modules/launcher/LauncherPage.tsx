import { useQuery } from '@tanstack/react-query'
import {
  Boxes,
  Receipt,
  SlidersHorizontal,
  UserRound,
  Users,
  Wallet,
} from 'lucide-react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { launcherApi, launcherKeys } from '@/app/api/launcher'
import { useSession } from '@/app/session/SessionContext'
import { Badge } from '@/components/ui'

import { ModuleCard, type ModuleCardModel } from './ModuleCard'

import './LauncherPage.css'

export function LauncherPage() {
  const { t } = useTranslation()
  const { session } = useSession()
  const navigate = useNavigate()
  const roles = session?.roles ?? []

  // Per-module attention. A failed or pending request simply shows no indicator;
  // attention is advisory and must never block reaching a module.
  const attention = useQuery({
    queryKey: launcherKeys.attention(),
    queryFn: ({ signal }) => launcherApi.attention(signal),
  })
  const attentionModules = attention.data?.modules

  const modules = useMemo<ModuleCardModel[]>(() => {
    const requiresAttention = (key: string) =>
      attentionModules?.find((module) => module.module === key)?.requiresAttention ??
      false
    return [
      {
        key: 'capex',
        title: t('capex:launcher.title'),
        description: t('capex:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/capex',
        icon: Wallet,
        tone: 'azure',
        attention: requiresAttention('capex'),
        attentionLabel: t('capex:launcher.attention'),
      },
      {
        key: 'opex',
        title: t('opex:launcher.title'),
        description: t('opex:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/opex',
        icon: Receipt,
        tone: 'sea',
      },
      {
        key: 'inventory',
        title: t('inventory:launcher.title'),
        description: t('inventory:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/inventory',
        icon: Boxes,
        tone: 'rose',
        attention: requiresAttention('inventory'),
        attentionLabel: t('inventory:launcher.attention'),
      },
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
      {
        key: 'configuration',
        title: t('launcher.modules.configuration.title'),
        description: t('launcher.modules.configuration.description'),
        actionLabel: t('launcher.open'),
        href: '/configuration',
        icon: SlidersHorizontal,
        tone: 'azure',
        requiresRole: 'Admin',
      },
    ]
  }, [t, attentionModules])
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
