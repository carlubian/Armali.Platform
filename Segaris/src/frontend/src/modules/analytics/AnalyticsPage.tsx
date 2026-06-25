import {
  Boxes,
  CalendarCheck,
  ChevronLeft,
  ChevronRight,
  Euro,
  LayoutDashboard,
  type LucideIcon,
  Luggage,
  Receipt,
  Shuffle,
  Wallet,
} from 'lucide-react'
import type { ComponentType } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'

import {
  analyticsMaximumYear,
  analyticsMinimumYear,
  analyticsTabs,
  type AnalyticsTab,
} from '@/app/api/analytics'

import {
  AnalyticsCapexPanel,
  AnalyticsCrossModulePanel,
  AnalyticsInventoryPanel,
  AnalyticsOpexPanel,
  AnalyticsOverviewPanel,
  AnalyticsTravelPanel,
} from './AnalyticsPanels'
import {
  currentAnalyticsYear,
  nextAnalyticsYear,
  parseAnalyticsState,
  previousAnalyticsYear,
} from './analyticsState'

import './AnalyticsPage.css'

const tabIcons: Record<AnalyticsTab, LucideIcon> = {
  overview: LayoutDashboard,
  capex: Wallet,
  opex: Receipt,
  inventory: Boxes,
  travel: Luggage,
  'cross-module': Shuffle,
}

const tabPanels: Record<
  AnalyticsTab,
  ComponentType<{ year: number; onConfigure?: () => void }>
> = {
  overview: AnalyticsOverviewPanel,
  capex: AnalyticsCapexPanel,
  opex: AnalyticsOpexPanel,
  inventory: AnalyticsInventoryPanel,
  travel: AnalyticsTravelPanel,
  'cross-module': AnalyticsCrossModulePanel,
}

export function AnalyticsPage() {
  const { t } = useTranslation('analytics')
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { year, tab } = parseAnalyticsState(searchParams)
  const currentYear = currentAnalyticsYear()

  function update(next: { year?: number; tab?: AnalyticsTab }) {
    setSearchParams({ year: String(next.year ?? year), tab: next.tab ?? tab })
  }

  const Panel = tabPanels[tab]

  return (
    <main className="seg-analytics armali-aurora">
      <div className="an-subbar">
        <div className="an-tabs" role="tablist" aria-label={t('tabs.label')}>
          {analyticsTabs.map((tabKey) => {
            const Icon = tabIcons[tabKey]
            const isActive = tabKey === tab
            return (
              <button
                key={tabKey}
                type="button"
                role="tab"
                aria-selected={isActive}
                className={['an-tab', isActive ? 'is-active' : '']
                  .filter(Boolean)
                  .join(' ')}
                onClick={() => update({ tab: tabKey })}
              >
                <Icon size={15} />
                {t(`tabs.${tabKey}`)}
              </button>
            )
          })}
        </div>
        <div className="an-subbar__right">
          <span className="an-eur">
            <Euro size={12} aria-hidden="true" />
            {t('eur.label')}
          </span>
          <div className="an-yearnav" role="group" aria-label={t('yearNav.label')}>
            <button
              type="button"
              className="an-yearnav__btn"
              aria-label={t('yearNav.previous')}
              disabled={year <= analyticsMinimumYear}
              onClick={() => update({ year: previousAnalyticsYear(year) })}
            >
              <ChevronLeft size={18} aria-hidden="true" />
            </button>
            <div className="an-yearnav__label">
              {year}
              <small>{t('yearNav.comparison', { year: year - 1 })}</small>
            </div>
            <button
              type="button"
              className="an-yearnav__btn"
              aria-label={t('yearNav.next')}
              disabled={year >= analyticsMaximumYear}
              onClick={() => update({ year: nextAnalyticsYear(year) })}
            >
              <ChevronRight size={18} aria-hidden="true" />
            </button>
            <button
              type="button"
              className="an-thisyear"
              disabled={year === currentYear}
              onClick={() => update({ year: currentYear })}
            >
              <CalendarCheck size={14} aria-hidden="true" />
              {t('yearNav.current')}
            </button>
          </div>
        </div>
      </div>
      <div className="an-page">
        <Panel
          year={year}
          onConfigure={() => void navigate('/configuration/global?catalog=currencies')}
        />
      </div>
    </main>
  )
}
