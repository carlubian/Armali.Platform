import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'

import { moodLogRoutePath } from '@/app/api/mood'
import { Button } from '@/components/ui'

import { MoodShell } from './MoodShell'

/**
 * Placeholder Dashboard view. Wave 5 replaces this with the strict-period trend
 * charts; for now it keeps the Log ⇄ Dashboard navigation whole and points the
 * user back to the working Log experience.
 */
export function MoodDashboardPage() {
  const { t } = useTranslation('mood')
  const navigate = useNavigate()
  return (
    <MoodShell
      eyebrow={t('dashboard.placeholder.eyebrow')}
      title={t('dashboard.placeholder.title')}
      description={t('dashboard.placeholder.description')}
    >
      <div className="mood-placeholder">
        <p>{t('dashboard.placeholder.body')}</p>
        <Button variant="outline" onClick={() => void navigate(moodLogRoutePath)}>
          {t('dashboard.placeholder.backToLog')}
        </Button>
      </div>
    </MoodShell>
  )
}
