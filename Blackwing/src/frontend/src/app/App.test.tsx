import { render, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'

import { App, appQueryClient } from '@/app/App'

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

describe('application shell and routing', () => {
  it('renders the boot screen inside the shell at the root route', () => {
    render(<App />)
    expect(
      screen.getByRole('navigation', { name: 'Navegación principal' }),
    ).toBeVisible()
    expect(screen.getByRole('heading', { level: 1, name: 'Galería' })).toBeVisible()
    expect(screen.getByText('Cimientos listos')).toBeVisible()
  })

  it('redirects an unknown path back to the root route', () => {
    window.history.replaceState({}, '', '/no-existe')
    render(<App />)
    expect(screen.getByText('Cimientos listos')).toBeVisible()
    expect(window.location.pathname).toBe('/')
  })

  it('provides a single query client to the tree', () => {
    expect(appQueryClient.getDefaultOptions().queries?.refetchOnWindowFocus).toBe(false)
    expect(appQueryClient.getDefaultOptions().mutations?.retry).toBe(false)
  })
})
