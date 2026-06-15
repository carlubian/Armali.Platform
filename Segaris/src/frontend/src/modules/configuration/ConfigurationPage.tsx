import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useParams, useSearchParams, useNavigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import { AccessDenied } from '@/components/feedback/SystemScreens'
import { Tabs, Toast } from '@/components/ui'

import { CatalogSection, type CatalogToastKind } from './CatalogSection'
import {
  categoriesDescriptor,
  defaultGlobalSlug,
  globalCatalogBySlug,
  globalCatalogs,
  type CatalogSectionId,
} from './catalogs'

import './ConfigurationPage.css'

const sections: CatalogSectionId[] = ['global', 'capex']

interface ToastState {
  kind: CatalogToastKind
  name: string
}

const globalHome = `/configuration/global?catalog=${defaultGlobalSlug}`

/**
 * The administrative Configuration experience: flat Global and Capex sections.
 * The active Global catalog is URL-backed via `?catalog=`; unknown sections or
 * catalogs fall back to Global Suppliers. The route and this page are restricted
 * to administrators.
 */
export function ConfigurationPage() {
  const { t } = useTranslation('configuration')
  const { session } = useSession()
  const navigate = useNavigate()
  const params = useParams<{ section?: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const [toast, setToast] = useState<ToastState | null>(null)

  const isAdmin = session?.roles.includes('Admin') ?? false
  if (!isAdmin) return <AccessDenied />

  const section = params.section
  // A bare or unknown section falls back to Global Suppliers.
  if (section == null || !sections.includes(section as CatalogSectionId)) {
    return <Navigate to={globalHome} replace />
  }

  const handleToast = (kind: CatalogToastKind, name: string) => setToast({ kind, name })

  const sectionTabs = sections.map((id) => ({
    value: id,
    label: t(`sections.${id}`),
  }))

  const changeSection = (next: string) => {
    if (next === section) return
    void navigate(next === 'global' ? globalHome : `/configuration/${next}`)
  }

  let body: ReactNode
  if (section === 'global') {
    const slug = searchParams.get('catalog')
    const descriptor = globalCatalogBySlug(slug)
    // An unknown catalog slug falls back to the default Global tab.
    if (descriptor == null) {
      return <Navigate to={globalHome} replace />
    }
    body = (
      <>
        <Tabs
          aria-label={t('catalogs.label')}
          value={descriptor.urlSlug}
          onChange={(next) => setSearchParams({ catalog: next })}
          tabs={globalCatalogs.map((catalog) => ({
            value: catalog.urlSlug as string,
            label: t(`catalogs.${catalog.key}.tab`),
          }))}
        />
        <CatalogSection
          key={descriptor.key}
          descriptor={descriptor}
          onToast={handleToast}
        />
      </>
    )
  } else {
    body = (
      <CatalogSection
        key={categoriesDescriptor.key}
        descriptor={categoriesDescriptor}
        onToast={handleToast}
      />
    )
  }

  return (
    <main className="seg-configuration armali-aurora">
      <section className="seg-configuration__head">
        <div className="armali-eyebrow">{t('page.eyebrow')}</div>
        <h1>{t('page.title')}</h1>
        <p>{t('page.description')}</p>
      </section>

      <Tabs
        variant="line"
        aria-label={t('sections.label')}
        value={section}
        onChange={changeSection}
        tabs={sectionTabs}
      />

      <div className="seg-configuration__body">{body}</div>

      {toast != null && (
        <div className="seg-configuration__toast">
          <Toast
            tone="success"
            title={t(`toast.${toast.kind}`)}
            onClose={() => setToast(null)}
            closeLabel={t('toast.close')}
          >
            {t(`toast.${toast.kind}Body`, { name: toast.name })}
          </Toast>
        </div>
      )}
    </main>
  )
}
