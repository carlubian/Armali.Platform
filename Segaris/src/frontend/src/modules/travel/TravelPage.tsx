import { useTranslation } from 'react-i18next'

export function TravelPage() {
  const { t } = useTranslation('travel')

  return (
    <main className="armali-aurora">
      <section className="seg-page-head">
        <div>
          <div className="armali-eyebrow">{t('page.eyebrow')}</div>
          <h1>{t('page.title')}</h1>
          <p>{t('page.description')}</p>
        </div>
      </section>
    </main>
  )
}
