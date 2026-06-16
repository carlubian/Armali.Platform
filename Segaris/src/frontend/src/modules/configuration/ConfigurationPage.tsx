import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useParams, useSearchParams, useNavigate } from 'react-router-dom'

import { useSession } from '@/app/session/SessionContext'
import { AccessDenied } from '@/components/feedback/SystemScreens'
import { Tabs, Toast } from '@/components/ui'

import { CatalogSection, type CatalogToastKind } from './CatalogSection'
import {
  catalogBySlug,
  defaultSlugForSection,
  sectionCatalogs,
  type CatalogSectionId,
} from './catalogs'

import './ConfigurationPage.css'

const sections: CatalogSectionId[] = ['global', 'capex', 'inventory']

interface ToastState {
  kind: CatalogToastKind
  name: string
}

/** The route a section lands on, including the default catalog tab when it has tabs. */
function sectionHome(section: CatalogSectionId): string {
  const slug = defaultSlugForSection(section)
  const catalogs = sectionCatalogs(section)
  return catalogs.length > 1 && slug != null
    ? `/configuration/${section}?catalog=${slug}`
    : `/configuration/${section}`
}

const globalHome = sectionHome('global')

/**
 * The administrative Configuration experience: flat Global, Capex, and Inventory
 * sections. Multi-catalog sections expose a URL-backed `?catalog=` tab; unknown
 * sections or catalogs fall back to Global Suppliers. The route and this page are
 * restricted to administrators.
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
  const activeSection = section as CatalogSectionId

  const handleToast = (kind: CatalogToastKind, name: string) => setToast({ kind, name })

  const sectionTabs = sections.map((id) => ({
    value: id,
    label: t(`sections.${id}`),
  }))

  const changeSection = (next: string) => {
    if (next === activeSection) return
    void navigate(sectionHome(next as CatalogSectionId))
  }

  const catalogs = sectionCatalogs(activeSection)
  let body: ReactNode
  if (catalogs.length > 1) {
    const slug = searchParams.get('catalog')
    const descriptor = catalogBySlug(activeSection, slug)
    // An unknown catalog slug falls back to the section's default tab.
    if (descriptor == null) {
      return <Navigate to={sectionHome(activeSection)} replace />
    }
    body = (
      <>
        <Tabs
          aria-label={t('catalogs.label')}
          value={descriptor.urlSlug}
          onChange={(next) => setSearchParams({ catalog: next })}
          tabs={catalogs.map((catalog) => ({
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
        key={catalogs[0].key}
        descriptor={catalogs[0]}
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
        value={activeSection}
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
