import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, MapPin } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { destinationsApi, destinationsRoutePath } from '@/app/api/destinations'
import { isApiError } from '@/app/api/errors'
import { ServiceUnavailable } from '@/components/feedback/SystemScreens'
import { Badge, Spinner } from '@/components/ui'

import { destinationsKeys } from './queries'

import './DestinationsPage.css'

export function DestinationPlacesPage() {
  const { t } = useTranslation('destinations')
  const params = useParams<{ destinationId: string }>()
  const destinationId = Number(params.destinationId)
  const validDestinationId = Number.isInteger(destinationId) && destinationId > 0

  const destinationQuery = useQuery({
    queryKey: destinationsKeys.destination(destinationId),
    queryFn: ({ signal }) => destinationsApi.getDestination(destinationId, signal),
    enabled: validDestinationId,
  })

  if (!validDestinationId) {
    return (
      <main className="seg-destinations armali-aurora">
        <section className="seg-destinations__empty" role="alert">
          {t('places.notFound')}
        </section>
      </main>
    )
  }

  if (destinationQuery.isError) {
    const error = destinationQuery.error
    if (isApiError(error) && ['unavailable', 'transient'].includes(error.kind)) {
      return <ServiceUnavailable onRetry={() => void destinationQuery.refetch()} />
    }
  }

  const destination = destinationQuery.data

  return (
    <main className="seg-destinations armali-aurora">
      <section className="seg-destinations__head">
        <div>
          <div className="armali-eyebrow">{t('places.eyebrow')}</div>
          <h1>
            {destination == null
              ? t('places.titleFallback')
              : t('places.title', { name: destination.name })}
          </h1>
          <p>{t('places.description')}</p>
        </div>
        <Link className="seg-destinations__view-link" to={destinationsRoutePath}>
          <ArrowLeft size={16} aria-hidden="true" />
          {t('places.backToGallery')}
        </Link>
      </section>

      {destinationQuery.isPending ? (
        <div className="seg-destinations__loading">
          <Spinner label={t('places.loading')} />
        </div>
      ) : destinationQuery.isError ? (
        <p className="seg-destinations__error" role="alert">
          {isApiError(destinationQuery.error) &&
          destinationQuery.error.kind === 'not-found'
            ? t('places.notFound')
            : t('places.loadError')}
        </p>
      ) : (
        <section className="seg-destinations__places-shell">
          <Badge tone="aqua">
            <MapPin size={14} aria-hidden="true" />
            {destination?.country ?? t('common.none')}
          </Badge>
          <p>{t('places.wave7Notice')}</p>
        </section>
      )}
    </main>
  )
}
