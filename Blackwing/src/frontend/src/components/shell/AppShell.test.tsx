import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'

import '@/app/i18n/i18n'
import { AppShell } from '@/components/shell/AppShell'

function renderShell(initialPath = '/') {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<AppShell />}>
          <Route index element={<p>contenido</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

describe('AppShell', () => {
  it('renders the brand and the five prototype navigation entries in order', () => {
    const { container } = renderShell()
    expect(screen.getByText('Blackwing')).toBeInTheDocument()

    const navigation = screen.getByRole('navigation', { name: 'Navegación principal' })
    expect(navigation).toBeInTheDocument()
    const entries = [...container.querySelectorAll('.bw-nav__item')].map(
      (element) => element.textContent,
    )
    expect(entries).toEqual(['Galería', 'Subir', 'Revisión', 'Tags', 'Ajustes'])
  })

  it('links the gallery and disables the sections that belong to later phases', () => {
    renderShell()
    const navigation = screen.getByRole('navigation', { name: 'Navegación principal' })

    expect(within(navigation).getByRole('link', { name: 'Galería' })).toHaveAttribute(
      'href',
      '/',
    )
    for (const label of ['Subir', 'Revisión', 'Tags', 'Ajustes']) {
      expect(within(navigation).getByRole('button', { name: label })).toBeDisabled()
    }
  })

  it('marks the active entry and titles the topbar from it', () => {
    renderShell()
    expect(screen.getByRole('link', { name: 'Galería' })).toHaveClass('active')
    expect(screen.getByRole('heading', { level: 1, name: 'Galería' })).toBeVisible()
    expect(screen.getByText('Todo tu archivo')).toBeVisible()
  })

  it('renders the routed screen inside the aurora content area', () => {
    const { container } = renderShell()
    const body = container.querySelector('.bw-body')
    expect(body).not.toBeNull()
    expect(body).toHaveClass('armali-aurora')
    expect(within(body as HTMLElement).getByText('contenido')).toBeInTheDocument()
  })
})
