import { render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'
import type { LauncherAttentionResponse } from '@/app/api/launcher'

const session = {
  userId: 7,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['User'],
  avatarUrl: null as string | null,
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function urlOf(input: RequestInfo | URL): string {
  return typeof input === 'string'
    ? input
    : input instanceof URL
      ? input.href
      : input.url
}

function mockBackend(attention: LauncherAttentionResponse) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    await Promise.resolve()
    const url = urlOf(input)
    const method = init?.method ?? 'GET'

    if (url === '/api/session/antiforgery') return json({ csrfToken: 'token' })
    if (url === '/api/session' && method === 'GET') return json(session)
    if (url === '/api/session/profile' && method === 'GET') {
      return json({
        displayName: session.displayName,
        language: session.language,
        avatarUrl: session.avatarUrl,
      })
    }
    if (url.startsWith('/api/launcher/attention')) return json(attention)

    throw new Error(`Unexpected request: ${method} ${url}`)
  })
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/')
})

afterEach(() => vi.restoreAllMocks())

describe('LauncherPage attention', () => {
  it('marks the Capex card when the backend reports attention', async () => {
    mockBackend({ modules: [{ module: 'capex', requiresAttention: true }] })
    render(<App />)

    expect(
      await screen.findByRole('status', {
        name: 'Overdue planned movements need attention.',
      }),
    ).toBeVisible()
  })

  it('shows no Capex indicator when attention is clear', async () => {
    mockBackend({ modules: [{ module: 'capex', requiresAttention: false }] })
    render(<App />)

    // The launcher renders before attention resolves; wait for the card, then
    // confirm the indicator stays absent.
    await screen.findByRole('button', { name: /Capex/i })
    await waitFor(() =>
      expect(
        screen.queryByRole('status', {
          name: 'Overdue planned movements need attention.',
        }),
      ).not.toBeInTheDocument(),
    )
  })
})
