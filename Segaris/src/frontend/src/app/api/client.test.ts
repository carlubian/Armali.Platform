import { afterEach, describe, expect, it, vi } from 'vitest'

import { apiRequest, resetCsrfToken, SESSION_EXPIRED_EVENT } from './client'

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

describe('apiRequest', () => {
  it('obtains and attaches antiforgery headers to mutations', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(json({ csrfToken: 'csrf-value' }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))

    await apiRequest('/api/example', {
      method: 'POST',
      body: JSON.stringify({ value: 1 }),
    })

    const request = fetchMock.mock.calls[1][1]
    expect(new Headers(request?.headers).get('X-CSRF-TOKEN')).toBe('csrf-value')
  })

  it('announces an expired session for 401 responses', async () => {
    const listener = vi.fn()
    window.addEventListener(SESSION_EXPIRED_EVENT, listener)
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(json({}, 401))

    await expect(apiRequest('/api/private')).rejects.toMatchObject({
      kind: 'authentication-expired',
    })
    expect(listener).toHaveBeenCalledOnce()
    window.removeEventListener(SESSION_EXPIRED_EVENT, listener)
  })

  it('classifies an unreachable backend as unavailable', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))
    await expect(apiRequest('/api/session')).rejects.toMatchObject({
      kind: 'unavailable',
    })
  })
})
