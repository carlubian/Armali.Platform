import { useQuery } from '@tanstack/react-query'
import {
  Archive,
  Boxes,
  Contact,
  Hammer,
  ListChecks,
  Luggage,
  MapPinned,
  Network,
  Receipt,
  ScrollText,
  Shirt,
  SlidersHorizontal,
  Smile,
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
        key: 'travel',
        title: t('travel:launcher.title'),
        description: t('travel:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/travel',
        icon: Luggage,
        tone: 'sea',
        attention: requiresAttention('travel'),
        attentionLabel: t('travel:launcher.attention'),
      },
      {
        key: 'destinations',
        title: t('destinations:launcher.title'),
        description: t('destinations:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/destinations',
        icon: MapPinned,
        tone: 'aqua',
      },
      {
        key: 'clothes',
        title: t('clothes:launcher.title'),
        description: t('clothes:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/clothes',
        icon: Shirt,
        tone: 'gold',
      },
      {
        key: 'mood',
        title: t('mood:launcher.title'),
        description: t('mood:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/mood',
        icon: Smile,
        tone: 'aqua',
      },
      {
        key: 'assets',
        title: t('assets:launcher.title'),
        description: t('assets:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/assets',
        icon: Archive,
        tone: 'azure',
        attention: requiresAttention('assets'),
        attentionLabel: t('assets:launcher.attention'),
      },
      {
        key: 'recipes',
        title: t('recipes:launcher.title'),
        description: t('recipes:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/recipes',
        icon: ScrollText,
        tone: 'gold',
      },
      {
        key: 'projects',
        title: t('projects:launcher.title'),
        description: t('projects:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/projects',
        icon: Network,
        tone: 'aqua',
        attention: requiresAttention('projects'),
        attentionLabel: t('projects:launcher.attention'),
      },
      {
        key: 'maintenance',
        title: t('maintenance:launcher.title'),
        description: t('maintenance:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/maintenance',
        icon: Hammer,
        tone: 'rose',
        attention: requiresAttention('maintenance'),
        attentionLabel: t('maintenance:launcher.attention'),
      },
      {
        key: 'processes',
        title: t('processes:launcher.title'),
        description: t('processes:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/processes',
        icon: ListChecks,
        tone: 'azure',
        attention: requiresAttention('processes'),
        attentionLabel: t('processes:launcher.attention'),
      },
      {
        key: 'firebird',
        title: t('firebird:launcher.title'),
        description: t('firebird:launcher.description'),
        actionLabel: t('launcher.open'),
        href: '/people',
        icon: Contact,
        tone: 'sea',
        attention: requiresAttention('firebird'),
        attentionLabel: t('firebird:launcher.attention'),
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
