import { afterEach, describe, expect, it, vi } from 'vitest'

import { apiRequest, resetCsrfToken } from './client'
import { sessionApi } from './session'

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

afterEach(() => {
  resetCsrfToken()
  vi.restoreAllMocks()
})

describe('sessionApi.signIn', () => {
  it('discards the anonymous antiforgery token so the next mutation re-fetches one', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      // Antiforgery token fetched while still anonymous for the login request.
      .mockResolvedValueOnce(json({ csrfToken: 'anonymous-token' }))
      // The login itself.
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      // A fresh antiforgery token must be fetched for the next mutation.
      .mockResolvedValueOnce(json({ csrfToken: 'authenticated-token' }))
      // The subsequent mutation.
      .mockResolvedValueOnce(new Response(null, { status: 204 }))

    await sessionApi.signIn({ userName: 'user', password: 'secret' })
    await apiRequest('/api/example', {
      method: 'POST',
      body: JSON.stringify({ value: 1 }),
    })

    const antiforgeryCalls = fetchMock.mock.calls.filter(
      ([input]) => String(input) === '/api/session/antiforgery',
    )
    expect(antiforgeryCalls).toHaveLength(2)

    const secondMutation = fetchMock.mock.calls[3][1]
    expect(new Headers(secondMutation?.headers).get('X-CSRF-TOKEN')).toBe(
      'authenticated-token',
    )
  })
})
