import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { App, appQueryClient } from '@/app/App'

const initialSession = {
  userId: 1,
  userName: 'marina',
  displayName: 'Marina Velasco',
  language: 'en-GB',
  roles: ['Admin'],
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

function mockProfileBackend() {
  const state = { ...initialSession }
  const requests: Array<{ method: string; url: string; body?: unknown }> = []

  const fetchMock = vi
    .spyOn(globalThis, 'fetch')
    .mockImplementation(async (input, init) => {
      await Promise.resolve()
      const url = urlOf(input)
      const method = init?.method ?? 'GET'

      if (url === '/api/session/antiforgery')
        return json({ csrfToken: 'profile-token' })
      if (url === '/api/session' && method === 'GET') return json(state)
      if (url === '/api/session/profile' && method === 'GET') {
        return json({
          displayName: state.displayName,
          language: state.language,
          avatarUrl: state.avatarUrl,
        })
      }
      if (url === '/api/session/profile' && method === 'PUT') {
        const body = JSON.parse(init?.body as string) as {
          displayName: string
          language: string
        }
        requests.push({ method, url, body })
        state.displayName = body.displayName
        state.language = body.language
        return json({ ...body, avatarUrl: state.avatarUrl })
      }
      if (url === '/api/session/password' && method === 'POST') {
        const body = JSON.parse(init?.body as string) as {
          currentPassword: string
          newPassword: string
        }
        requests.push({ method, url, body })
        if (body.currentPassword === 'wrong-password') {
          return json(
            {
              code: 'request.invalid',
              errors: { newPassword: ['Passwords do not match.'] },
            },
            400,
          )
        }
        return new Response(null, { status: 204 })
      }
      if (url === '/api/session/profile/avatar' && method === 'PUT') {
        const body = init?.body as FormData
        const file = body.get('file') as File
        requests.push({ method, url, body: file })
        if (file.name === 'rejected.png') {
          return json(
            {
              code: 'request.invalid',
              errors: { file: ['Invalid image signature.'] },
            },
            400,
          )
        }
        state.avatarUrl = '/api/users/1/avatar'
        return json({
          avatarUrl: state.avatarUrl,
          contentType: file.type,
          size: file.size,
        })
      }
      if (url === '/api/session/profile/avatar' && method === 'DELETE') {
        requests.push({ method, url })
        state.avatarUrl = null
        return new Response(null, { status: 204 })
      }

      throw new Error(`Unexpected request: ${method} ${url}`)
    })

  return { fetchMock, requests }
}

beforeEach(() => {
  appQueryClient.clear()
  window.history.replaceState({}, '', '/profile')
})

afterEach(() => vi.restoreAllMocks())

describe('profile screen', () => {
  it('updates profile details and reflects the display name in the shared shell', async () => {
    const user = userEvent.setup()
    const { requests } = mockProfileBackend()
    render(<App />)

    const displayName = await screen.findByLabelText('Display name')
    await user.clear(displayName)
    await user.type(displayName, 'Marina Harbour')
    await user.click(screen.getByRole('button', { name: 'Save profile' }))

    await waitFor(() =>
      expect(screen.getAllByRole('img', { name: 'Marina Harbour' })).toHaveLength(2),
    )
    expect(requests).toContainEqual({
      method: 'PUT',
      url: '/api/session/profile',
      body: { displayName: 'Marina Harbour', language: 'en-GB' },
    })
    expect(screen.getByRole('button', { name: 'Save profile' })).toBeDisabled()
  })

  it('changes the password, keeps the session active, and maps a rejection to the form', async () => {
    const user = userEvent.setup()
    mockProfileBackend()
    render(<App />)

    await screen.findByRole('heading', { name: 'My profile' })
    await user.type(screen.getByLabelText('Current password'), 'CurrentPass123!')
    await user.type(screen.getByLabelText('New password'), 'NewPassphrase123!')
    await user.type(screen.getByLabelText('Confirm new password'), 'NewPassphrase123!')
    await user.click(screen.getByRole('button', { name: 'Change password' }))

    expect(
      await screen.findByText(
        'Your password is updated and this session remains active.',
      ),
    ).toBeInTheDocument()
    expect(window.location.pathname).toBe('/profile')

    await user.type(screen.getByLabelText('Current password'), 'wrong-password')
    await user.type(screen.getByLabelText('New password'), 'AnotherPass123!')
    await user.type(screen.getByLabelText('Confirm new password'), 'AnotherPass123!')
    await user.click(screen.getByRole('button', { name: 'Change password' }))

    const currentPassword = screen.getByLabelText('Current password')
    await waitFor(() => expect(currentPassword).toHaveFocus())
    expect(currentPassword).toHaveAccessibleDescription(
      'Check your current password and choose a valid new password.',
    )
  })

  it('uploads, cache-busts, and removes the avatar across the profile and shell', async () => {
    const user = userEvent.setup()
    const { requests } = mockProfileBackend()
    render(<App />)

    const fileInput = await screen.findByLabelText('Choose a profile photo')
    await user.upload(fileInput, new File(['png'], 'avatar.png', { type: 'image/png' }))

    await waitFor(() => {
      const images = screen.getAllByAltText('Marina Velasco')
      expect(images).toHaveLength(2)
      expect(images[0].getAttribute('src')).toMatch(/^\/api\/users\/1\/avatar\?v=\d+$/)
    })
    await user.click(screen.getByRole('button', { name: 'Remove photo' }))

    await waitFor(() =>
      expect(screen.getAllByRole('img', { name: 'Marina Velasco' })).toHaveLength(2),
    )
    expect(requests.filter((request) => request.url.includes('avatar'))).toHaveLength(2)
  })

  it('rejects invalid avatars on the client and translates backend rejection', async () => {
    const { requests } = mockProfileBackend()
    render(<App />)

    const fileInput = await screen.findByLabelText('Choose a profile photo')
    fireEvent.change(fileInput, {
      target: {
        files: [new File(['pdf'], 'document.pdf', { type: 'application/pdf' })],
      },
    })
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Choose a JPEG, PNG, or WebP image.',
    )
    expect(requests).toHaveLength(0)

    fireEvent.change(fileInput, {
      target: {
        files: [new File(['not-a-png'], 'rejected.png', { type: 'image/png' })],
      },
    })
    await waitFor(() =>
      expect(requests.filter((request) => request.method === 'PUT')).toHaveLength(1),
    )
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Choose a JPEG, PNG, or WebP image.',
    )
  })

  it('blocks internal navigation while either form has unsaved changes', async () => {
    const user = userEvent.setup()
    mockProfileBackend()
    render(<App />)

    const displayName = await screen.findByLabelText('Display name')
    await user.type(displayName, ' changed')
    await user.click(screen.getByRole('button', { name: 'Launcher' }))

    expect(
      await screen.findByRole('dialog', { name: 'Discard unsaved changes?' }),
    ).toBeInTheDocument()
    expect(window.location.pathname).toBe('/profile')

    await user.click(screen.getByRole('button', { name: 'Discard and leave' }))
    expect(
      await screen.findByRole('heading', { name: 'Choose a module' }),
    ).toBeInTheDocument()
    expect(window.location.pathname).toBe('/')
  })
})
