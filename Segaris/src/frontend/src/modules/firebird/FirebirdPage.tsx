import { useTranslation } from 'react-i18next'

export function FirebirdPage() {
  const { t } = useTranslation('firebird')

  return (
    <main className="module-page" aria-labelledby="firebird-title">
      <p className="module-eyebrow">{t('page.eyebrow')}</p>
      <h1 id="firebird-title">{t('page.title')}</h1>
      <p>{t('page.description')}</p>
      <p>{t('page.pending')}</p>
    </main>
  )
}
