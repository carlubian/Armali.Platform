import { render, screen } from '@testing-library/react'
import axe from 'axe-core'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import { platform } from '@/app/i18n/resources'
import { adminSession, mockBackend } from '@/test/backend'

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('core screen accessibility', () => {
  it('has no automated violations on the boot screen', async () => {
    mockBackend({ session: adminSession })
    render(<App />)
    await screen.findByRole('heading', {
      level: 1,
      name: platform.shell.nav.gallery.title,
    })

    // Colour contrast is excluded: jsdom does not apply the stylesheets, so
    // every computed colour would be the browser default.
    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })

    expect(result.violations).toEqual([])
  })

  it('has no automated violations on the login screen', async () => {
    window.history.replaceState({}, '', '/login')
    mockBackend({ session: null })
    render(<App />)
    await screen.findByRole('heading', { name: platform.auth.login.title })

    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })

    expect(result.violations).toEqual([])
  })

  it('has no automated violations on the account administration screen', async () => {
    window.history.replaceState({}, '', '/admin/users')
    mockBackend({ session: adminSession })
    render(<App />)
    await screen.findAllByRole('listitem')

    const result = await axe.run(document.body, {
      rules: { 'color-contrast': { enabled: false } },
    })

    expect(result.violations).toEqual([])
  })
})
