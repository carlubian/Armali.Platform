import { Check } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

import { Button, Card, Spinner } from '@/components/ui'

import './StartupPage.css'

/**
 * Minimal boot screen for the frontend foundations.
 *
 * It exercises the pieces this milestone delivers — routing, the query client,
 * the shell, the aurora background, and the ported primitives — and is replaced
 * by the real gallery in a later phase.
 */
export function StartupPage() {
  const { t } = useTranslation('platform')
  const [acknowledged, setAcknowledged] = useState(false)

  return (
    <div className="bw-startup">
      <Card
        className="bw-startup__card"
        glass
        title={t('startup.cardTitle')}
        subtitle={t('startup.cardSubtitle')}
        footer={
          <Button
            variant="primary"
            iconLeft={<Check size={16} aria-hidden="true" />}
            onClick={() => setAcknowledged(true)}
            disabled={acknowledged}
          >
            {t('startup.action')}
          </Button>
        }
      >
        <p>{t('startup.body')}</p>
        {/* The Spinner already carries role="status"; nesting a second live
            region here would announce the same change twice. */}
        <p className="bw-startup__status">
          {acknowledged ? (
            t('startup.acknowledged')
          ) : (
            <>
              <Spinner size={18} label={t('common.loading')} />
              <span>{t('startup.working')}</span>
            </>
          )}
        </p>
      </Card>
    </div>
  )
}
