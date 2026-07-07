import { ArrowLeft } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'

import { isApiError } from '@/app/api/errors'
import { Badge, Button, Spinner } from '@/components/ui'

import { usePlaythrough } from './queries'

import './GamesPage.css'

/**
 * Playthrough-scoped progress page. Wave 4 wires the route, header context, and
 * back navigation; Wave 5 builds the two-pane section list and goal experience on
 * top of this shell.
 */
export function ProgressPage() {
  const { t } = useTranslation('games')
  const navigate = useNavigate()
  const params = useParams()
  const playthroughId = Number.parseInt(params.playthroughId ?? '', 10)
  const valid = Number.isFinite(playthroughId) && playthroughId > 0
  const query = usePlaythrough(valid ? playthroughId : 0, valid)

  const back = (
    <Button
      variant="outline"
      size="sm"
      iconLeft={<ArrowLeft size={15} />}
      onClick={() => void navigate('/games')}
    >
      {t('progressPage.back')}
    </Button>
  )

  if (!valid || query.isError) {
    const notFound =
      !valid || (isApiError(query.error) && query.error.kind === 'not-found')
    return (
      <main className="seg-games-progress armali-aurora">
        <div className="seg-games-progress__back">{back}</div>
        <p className="seg-games__error" role="alert">
          {notFound ? t('progressPage.notFound') : t('progressPage.loadError')}
        </p>
      </main>
    )
  }

  if (query.isPending) {
    return (
      <main className="seg-games-progress armali-aurora">
        <div className="seg-games-progress__back">{back}</div>
        <div className="seg-games-progress__status">
          <Spinner />
          <span>{t('progressPage.loading')}</span>
        </div>
      </main>
    )
  }

  const playthrough = query.data
  const { completedGoals, totalGoals } = playthrough.progress

  return (
    <main className="seg-games-progress armali-aurora">
      <div className="seg-games-progress__back">{back}</div>
      <section className="seg-games__head">
        <div>
          <div className="armali-eyebrow">{playthrough.gameName}</div>
          <h1>{playthrough.name}</h1>
        </div>
        <div className="seg-games__stats">
          <Badge tone={playthrough.status === 'Completed' ? 'success' : 'aqua'}>
            {t(`status.${playthrough.status}`)}
          </Badge>
          <Badge tone="neutral">
            {totalGoals === 0
              ? t('progress.none')
              : t('progress.count', { done: completedGoals, total: totalGoals })}
          </Badge>
        </div>
      </section>
    </main>
  )
}
