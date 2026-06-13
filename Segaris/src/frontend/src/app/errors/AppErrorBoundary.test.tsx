import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { AppErrorBoundary } from './AppErrorBoundary'
import '@/app/i18n/i18n'

let shouldThrow = true

function BrokenComponent() {
  if (shouldThrow) throw new Error('render failed')
  return <div>Recovered content</div>
}

describe('AppErrorBoundary', () => {
  beforeEach(() => {
    shouldThrow = true
    vi.spyOn(console, 'error').mockImplementation(() => undefined)
  })

  it('shows a recovery screen and retries only after user action', async () => {
    const user = userEvent.setup()
    render(
      <AppErrorBoundary>
        <BrokenComponent />
      </AppErrorBoundary>,
    )
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.queryByText('Recovered content')).not.toBeInTheDocument()
    shouldThrow = false
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    expect(screen.getByText('Recovered content')).toBeInTheDocument()
  })
})
