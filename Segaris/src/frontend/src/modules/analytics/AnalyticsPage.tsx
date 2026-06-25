import { BarChart3 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Badge } from '@/components/ui'

import { parseAnalyticsState } from './analyticsState'

export function AnalyticsPage() {
  const { t } = useTranslation('analytics')
  const state = parseAnalyticsState(new URLSearchParams(window.location.search))

  return (
    <main className="seg-analytics armali-aurora">
      <section className="seg-analytics__head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
        <Badge tone="neutral">
          <BarChart3 size={14} />
          {state.year}
        </Badge>
      </section>
    </main>
  )
}
