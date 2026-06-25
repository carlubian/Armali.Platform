import { CircleAlert, SlidersHorizontal } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { Button, Spinner } from '@/components/ui'

/** Centered spinner shown while a tab's data is loading. */
export function AnalyticsLoading() {
  const { t } = useTranslation('analytics')
  return (
    <div className="an-state" aria-busy="true">
      <Spinner size={24} label={t('states.loading')} />
      <span>{t('states.loading')}</span>
    </div>
  )
}

/** Error state with a retry affordance when a tab's request fails. */
export function AnalyticsError({ onRetry }: { onRetry: () => void }) {
  const { t } = useTranslation('analytics')
  return (
    <div className="an-state an-state--error" role="alert">
      <CircleAlert size={26} aria-hidden="true" />
      <p>{t('states.error')}</p>
      <Button variant="outline" size="sm" onClick={onRetry}>
        {t('states.retry')}
      </Button>
    </div>
  )
}

/**
 * Configuration-incomplete state. The backend cannot safely aggregate when a
 * currency has no current exchange rate to EUR, so it returns the affected
 * currency codes and zeroed charts; this surfaces those codes instead of
 * showing misleading empty charts. An administrator CTA links to currency
 * configuration when one is available.
 */
export function AnalyticsConfigurationIncomplete({
  currencyCodes,
  onConfigure,
}: {
  currencyCodes: string[]
  onConfigure?: () => void
}) {
  const { t } = useTranslation('analytics')
  return (
    <div className="an-state an-state--config" role="status">
      <SlidersHorizontal size={26} aria-hidden="true" />
      <h3>{t('states.configIncompleteTitle')}</h3>
      <p>{t('states.configIncompleteBody')}</p>
      <p className="an-state__codes">
        {t('states.configIncompleteCurrencies')}{' '}
        <strong>{currencyCodes.join(', ')}</strong>
      </p>
      {onConfigure != null && (
        <Button variant="outline" size="sm" onClick={onConfigure}>
          {t('states.configIncompleteCta')}
        </Button>
      )}
    </div>
  )
}
